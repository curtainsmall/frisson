namespace CoyoteStudio.Core.Control;

internal class Pulse
{
    private readonly List<int[]> _values = [];

    public bool Add(int[] values)
    {
        if (values.Length != 8)
            return false;

        _values.Add(values);
        return true;
    }

    public void Remove(int index)
    {
        _values.RemoveAt(index);
    }
}
