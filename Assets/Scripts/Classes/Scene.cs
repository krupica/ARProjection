using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Classes
{
    public class Scene
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Int_modified { get; set; }
        public string Id { get; set; }
        public Object[] Objects { get; set; }
    }
}
