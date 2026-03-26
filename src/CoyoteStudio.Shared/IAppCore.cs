using System;
using System.Collections.Generic;
using System.Text;

namespace CoyoteStudio.Shared;

public interface IAppCore
{
    public void StartServerAsync(int port);
}
