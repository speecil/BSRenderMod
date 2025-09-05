using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RenderMod.UI
{
    internal class DependantInteractable : MonoBehaviour
    {
        public delegate bool InteractableDelegate();
        public Button Dependant = null;

        public InteractableDelegate interactableCheck;

        void Update()
        {
            if (Dependant != null && interactableCheck != null)
            {
                Dependant.interactable = !interactableCheck();
            }
        }
    }
}
