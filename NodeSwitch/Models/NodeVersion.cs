using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NodeSwitch.Models
{
    public class NodeVersion
    {
        public string Version { get; set; } = default!;
        public bool IsInstalled { get; set; }
        public bool IsActive { get; set; }

        public Visibility Visibility => IsActive ? Visibility.Collapsed : Visibility.Visible;
    }
}
