using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Classes
{
    public class Object
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Pose Pose { get; set; }
        public object[] Parameters { get; set; }
        public object[] Children { get; set; }
        public string Id { get; set; }

    }
}
