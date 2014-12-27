using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ConnectFour
{
    /// <summary>
    /// Type of the player.
    /// </summary>
    public enum PlayerType
    {
        [Description("Human")]
        Human = 0,
        [Description("Computer")]
        Computer = 1,
    }
}
