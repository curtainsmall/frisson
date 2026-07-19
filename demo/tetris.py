"""
Tetris — connects to Frisson as a Remote (Control Source).
When the game is lost, strength is set to the Remote UI preset value for 5 seconds, then back to 0.

Requirements:
    pip install pygame websockets

Usage:
    1. Start Frisson and accept the "Tetris" connection in the UI.
    2. Run: python tetris.py
"""

import asyncio
import json
import random
import sys
import threading
import time

import pygame

# ── Frisson WebSocket config ──────────────────────────────────────────────────
FRISSON_HOST = "ws://127.0.0.1:6969"
REMOTE_NAME = "Tetris"
GAME_OVER_STRENGTH = 50
GAME_OVER_DURATION = 5  # seconds

# ── Tetris constants ──────────────────────────────────────────────────────────
CELL = 30
COLS, ROWS = 10, 20
SIDEBAR = 200
WIDTH = CELL * COLS + SIDEBAR
HEIGHT = CELL * ROWS
FPS = 60

SHAPES = [
    [[1, 1, 1, 1]],                         # I
    [[1, 1], [1, 1]],                        # O
    [[0, 1, 1], [1, 1, 0]],                  # S
    [[1, 1, 0], [0, 1, 1]],                  # Z
    [[1, 0, 0], [1, 1, 1]],                  # J
    [[0, 0, 1], [1, 1, 1]],                  # L
    [[0, 1, 0], [1, 1, 1]],                  # T
]
COLORS = [
    (0, 240, 240),   # I - cyan
    (240, 240, 0),   # O - yellow
    (0, 240, 0),     # S - green
    (240, 0, 0),     # Z - red
    (0, 0, 240),     # J - blue
    (240, 160, 0),   # L - orange
    (160, 0, 240),   # T - purple
]

# ── Frisson connection (runs in background thread) ───────────────────────────

class FrissonLink:
    """Manages the WebSocket connection to Frisson in a background thread."""

    def __init__(self):
        self._ws = None
        self._loop = None
        self._thread = None
        self._connected = False
        self._bound = False
        self.paused = True
        # Strength values pre-configured via Remote UI; used on game over
        self.strength_a = GAME_OVER_STRENGTH
        self.strength_b = GAME_OVER_STRENGTH

    def start(self):
        self._thread = threading.Thread(target=self._run, daemon=True)
        self._thread.start()
        # Wait until bound or timeout
        for _ in range(100):
            if self._bound:
                return True
            if not self._connected and not self._thread.is_alive():
                break
            time.sleep(0.1)
        return self._bound

    def send_set(self, channel: str, value: int):
        if self._ws and self._loop and self._connected:
            msg = json.dumps({"type": "set", "channel": channel, "value": value})
            asyncio.run_coroutine_threadsafe(self._ws.send(msg), self._loop)

    def close(self):
        """Send WebSocket close frame and wait for it to complete."""
        if self._ws and self._loop:
            fut = asyncio.run_coroutine_threadsafe(self._ws.close(), self._loop)
            try:
                fut.result(timeout=3)  # wait for clean close
            except Exception:
                pass  # connection already closed or timeout

    def _run(self):
        self._loop = asyncio.new_event_loop()
        asyncio.set_event_loop(self._loop)
        self._loop.run_until_complete(self._connect())

    async def _connect(self):
        try:
            import websockets
        except ImportError:
            print("[Frisson] websockets not installed. Run: pip install websockets")
            return

        try:
            self._ws = await websockets.connect(FRISSON_HOST)
            self._connected = True
            print(f"[Frisson] Connected to {FRISSON_HOST}")

            # Send bind
            bind_msg = json.dumps({
                "type": "bind",
                "name": REMOTE_NAME,
                "alwaysReply": True,
                "ui": [
                    {"type": "number", "key": "strengthA", "label": "Strength A", "min": 0, "max": 100, "step": 1},
                    {"type": "number", "key": "strengthB", "label": "Strength B", "min": 0, "max": 100, "step": 1},
                ],
            })
            await self._ws.send(bind_msg)
            print(f"[Frisson] Sent bind as '{REMOTE_NAME}', waiting for confirmation...")

            # Wait for bind reply
            async for raw in self._ws:
                data = json.loads(raw)
                if data.get("type") == "bind" and "id" in data:
                    self._bound = True
                    print(f"[Frisson] Bound! Assigned ID: {data['id']}")
                    break
                elif data.get("type") == "error":
                    print(f"[Frisson] Bind rejected: {data.get('message', '')}")
                    return

            # Keep alive — read messages until ws closes
            async for raw in self._ws:
                try:
                    data = json.loads(raw)
                    msg_type = data.get("type", "")
                    if msg_type == "deactivated":
                        self.paused = True
                        print("[Frisson] Remote deactivated — game paused.")
                    elif msg_type == "activated":
                        self.paused = False
                        print("[Frisson] Remote activated — game resumed.")
                    elif msg_type == "ui":
                        key = data.get("key")
                        value = data.get("value")
                        if isinstance(value, (int, float)):
                            if key == "strengthA":
                                self.strength_a = int(value)
                                print(f"[Frisson] Strength A preset → {self.strength_a}")
                            elif key == "strengthB":
                                self.strength_b = int(value)
                                print(f"[Frisson] Strength B preset → {self.strength_b}")
                except (json.JSONDecodeError, KeyError):
                    pass

        except Exception as e:
            print(f"[Frisson] Connection failed: {e}")
        finally:
            self._connected = False
            if self._ws:
                await self._ws.close()


# ── Tetris piece ──────────────────────────────────────────────────────────────

class Piece:
    def __init__(self, shape_idx):
        self.shape = [row[:] for row in SHAPES[shape_idx]]
        self.color = COLORS[shape_idx]
        self.x = COLS // 2 - len(self.shape[0]) // 2
        self.y = 0

    def rotated(self):
        rows, cols = len(self.shape), len(self.shape[0])
        return [[self.shape[rows - 1 - j][i] for j in range(rows)] for i in range(cols)]


# ── Game ──────────────────────────────────────────────────────────────────────

class Tetris:
    def __init__(self, frisson: FrissonLink):
        self.frisson = frisson
        self.board = [[None] * COLS for _ in range(ROWS)]
        self.score = 0
        self.game_over = False
        self.bag = []
        self.piece = self._spawn()
        self.drop_interval = 0.5
        self.drop_timer = 0.0
        self.locked = False

    def _refill_bag(self):
        bag = list(range(len(SHAPES)))
        random.shuffle(bag)
        self.bag.extend(bag)

    def _spawn(self) -> Piece:
        if len(self.bag) < 1:
            self._refill_bag()
        return Piece(self.bag.pop())

    def _valid(self, piece, dx=0, dy=0, shape=None):
        s = shape or piece.shape
        for r, row in enumerate(s):
            for c, v in enumerate(row):
                if not v:
                    continue
                nx, ny = piece.x + c + dx, piece.y + r + dy
                if nx < 0 or nx >= COLS or ny >= ROWS:
                    return False
                if ny >= 0 and self.board[ny][nx] is not None:
                    return False
        return True

    def _lock(self):
        above_board = False
        for r, row in enumerate(self.piece.shape):
            for c, v in enumerate(row):
                if v:
                    y = self.piece.y + r
                    x = self.piece.x + c
                    if y < 0:
                        above_board = True
                    else:
                        self.board[y][x] = self.piece.color
        if above_board:
            self._on_game_over()
            return
        self._clear_lines()
        self.piece = self._spawn()
        if not self._valid(self.piece):
            self._on_game_over()

    def _clear_lines(self):
        cleared = 0
        new_board = []
        for row in self.board:
            if all(c is not None for c in row):
                cleared += 1
            else:
                new_board.append(row)
        for _ in range(cleared):
            new_board.insert(0, [None] * COLS)
        self.board = new_board
        self.score += [0, 100, 300, 500, 800][cleared]

    def _on_game_over(self):
        self.game_over = True
        print(f"[Tetris] Game Over! Score: {self.score}")
        # Set both channels to last-preset strength values from Remote UI
        self.frisson.send_set("A", self.frisson.strength_a)
        self.frisson.send_set("B", self.frisson.strength_b)
        # Reset both to 0 after 5 seconds
        def _reset():
            time.sleep(GAME_OVER_DURATION)
            self.frisson.send_set("A", 0)
            self.frisson.send_set("B", 0)
            print("[Frisson] Both channels reset to 0.")
        threading.Thread(target=_reset, daemon=True).start()

    def move(self, dx, dy):
        if self.game_over:
            return
        if self._valid(self.piece, dx, dy):
            self.piece.x += dx
            self.piece.y += dy
            return True
        return False

    def rotate(self):
        if self.game_over:
            return
        r = self.piece.rotated()
        if self._valid(self.piece, shape=r):
            self.piece.shape = r
        elif self._valid(self.piece, dx=1, shape=r):
            self.piece.x += 1
            self.piece.shape = r
        elif self._valid(self.piece, dx=-1, shape=r):
            self.piece.x -= 1
            self.piece.shape = r

    def hard_drop(self):
        if self.game_over:
            return
        while self._valid(self.piece, 0, 1):
            self.piece.y += 1
        self._lock()

    def restart(self):
        self.board = [[None] * COLS for _ in range(ROWS)]
        self.score = 0
        self.game_over = False
        self.bag = []
        self.piece = self._spawn()
        self.drop_timer = 0.0

    def update(self, dt):
        if self.game_over:
            return
        self.drop_timer += dt
        if self.drop_timer >= self.drop_interval:
            self.drop_timer = 0.0
            if not self.move(0, 1):
                self._lock()

    def draw(self, surface):
        # Background
        surface.fill((18, 18, 24))

        # Board area
        board_rect = pygame.Rect(0, 0, CELL * COLS, CELL * ROWS)
        pygame.draw.rect(surface, (30, 30, 40), board_rect)

        # Grid lines
        for c in range(COLS + 1):
            pygame.draw.line(surface, (45, 45, 55), (c * CELL, 0), (c * CELL, CELL * ROWS))
        for r in range(ROWS + 1):
            pygame.draw.line(surface, (45, 45, 55), (0, r * CELL), (CELL * COLS, r * CELL))

        # Locked cells
        for r in range(ROWS):
            for c in range(COLS):
                if self.board[r][c]:
                    self._draw_cell(surface, c, r, self.board[r][c])

        # Active piece (always drawn, even on game over so the final state is visible)
        for r, row in enumerate(self.piece.shape):
            for c, v in enumerate(row):
                if v:
                    py = self.piece.y + r
                    if py >= 0:
                        self._draw_cell(surface, self.piece.x + c, py, self.piece.color)

        # Sidebar
        sx = CELL * COLS + 20
        font = pygame.font.SysFont("consolas", 22)
        big_font = pygame.font.SysFont("consolas", 32, bold=True)

        # Title
        title = big_font.render("TETRIS", True, (255, 255, 255))
        surface.blit(title, (sx, 30))

        # Score
        score_label = font.render(f"Score: {self.score}", True, (200, 200, 200))
        surface.blit(score_label, (sx, 80))

        # Controls
        controls = [
            "← → : Move",
            "↑   : Rotate",
            "↓   : Soft drop",
            "Space: Hard drop",
            "R   : Restart",
        ]
        y = 140
        small_font = pygame.font.SysFont("consolas", 16)
        for line in controls:
            txt = small_font.render(line, True, (140, 140, 160))
            surface.blit(txt, (sx, y))
            y += 24

        # Connection status
        y += 20
        status_color = (0, 200, 0) if self.frisson._bound else (200, 0, 0)
        status_text = "Frisson: Connected" if self.frisson._bound else "Frisson: Disconnected"
        txt = small_font.render(status_text, True, status_color)
        surface.blit(txt, (sx, y))

        # Pause overlay
        if self.frisson.paused and not self.game_over:
            pause_text = big_font.render("PAUSED", True, (255, 200, 0))
            rect = pause_text.get_rect(center=(CELL * COLS // 2, CELL * ROWS // 2))
            surface.blit(pause_text, rect)

        # Game over — text hint only, no overlay covering the board
        if self.game_over:
            go_text = big_font.render("GAME OVER", True, (255, 60, 60))
            rect = go_text.get_rect(center=(CELL * COLS // 2, CELL * ROWS - 40))
            surface.blit(go_text, rect)
            hint = font.render("Press R to restart", True, (200, 200, 200))
            rect2 = hint.get_rect(center=(CELL * COLS // 2, CELL * ROWS - 12))
            surface.blit(hint, rect2)

        # Border
        pygame.draw.rect(surface, (80, 80, 100), board_rect, 2)

    def _draw_cell(self, surface, x, y, color):
        rect = pygame.Rect(x * CELL + 1, y * CELL + 1, CELL - 2, CELL - 2)
        pygame.draw.rect(surface, color, rect, border_radius=4)
        # Highlight
        lighter = tuple(min(c + 40, 255) for c in color)
        pygame.draw.line(surface, lighter, (rect.x + 2, rect.y + 2), (rect.right - 3, rect.y + 2))
        pygame.draw.line(surface, lighter, (rect.x + 2, rect.y + 2), (rect.x + 2, rect.bottom - 3))


# ── Main ──────────────────────────────────────────────────────────────────────

def main():
    pygame.init()
    screen = pygame.display.set_mode((WIDTH, HEIGHT))
    pygame.display.set_caption("Tetris × Frisson")
    clock = pygame.time.Clock()

    frisson = FrissonLink()
    connected = frisson.start()
    if not connected:
        print("[Frisson] Connection failed — exiting.")
        pygame.quit()
        sys.exit(1)
    print("[Frisson] Ready! Accept the connection in the Frisson UI if prompted.")

    game = Tetris(frisson)

    # DAS (delayed auto shift) for smooth horizontal movement
    das_delay = 0.15
    das_repeat = 0.05
    das_dir = 0
    das_timer = 0.0
    das_charged = False
    soft_drop = False

    running = True
    while running:
        dt = clock.tick(FPS) / 1000.0

        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                frisson.close()
                running = False

            elif event.type == pygame.KEYDOWN:
                if event.key == pygame.K_r:
                    game.restart()
                elif not frisson.paused:
                    if event.key == pygame.K_LEFT:
                        game.move(-1, 0)
                        das_dir = -1
                        das_timer = 0.0
                        das_charged = False
                    elif event.key == pygame.K_RIGHT:
                        game.move(1, 0)
                        das_dir = 1
                        das_timer = 0.0
                        das_charged = False
                    elif event.key == pygame.K_DOWN:
                        soft_drop = True
                    elif event.key == pygame.K_UP:
                        game.rotate()
                    elif event.key == pygame.K_SPACE:
                        game.hard_drop()

            elif event.type == pygame.KEYUP:
                if event.key in (pygame.K_LEFT, pygame.K_RIGHT):
                    das_dir = 0
                if event.key == pygame.K_DOWN:
                    soft_drop = False

        # Exit when Frisson disconnects
        if not frisson._connected:
            print("[Frisson] Connection lost — exiting.")
            running = False
            break

        if frisson.paused:
            game.draw(screen)
            pygame.display.flip()
            continue

        # DAS
        if das_dir != 0:
            das_timer += dt
            if not das_charged and das_timer >= das_delay:
                das_charged = True
                das_timer = 0.0
            elif das_charged and das_timer >= das_repeat:
                game.move(das_dir, 0)
                das_timer = 0.0

        # Soft drop speed
        if soft_drop and not game.game_over:
            game.drop_interval = 0.05
        else:
            game.drop_interval = 0.5

        game.update(dt)
        game.draw(screen)
        pygame.display.flip()

    pygame.quit()


if __name__ == "__main__":
    main()
