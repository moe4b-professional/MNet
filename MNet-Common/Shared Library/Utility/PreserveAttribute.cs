using System;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    /// <summary>
    /// Replacement for UnityEngine.Scripting.PreserveAttribute in the repository's code
    /// This works because the Unity Stripper Only Checks for Attribute Name
    /// So Since they have the same name, they end up having the same functionality
    /// </summary>
    [AttributeUsage(Targets, Inherited = false, AllowMultiple = false)]
    public sealed class PreserveAttribute : Attribute
    {
        public const AttributeTargets Targets = AttributeTargets.Method | AttributeTargets.Class |
            AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Constructor |
            AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Event |
            AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Enum;
    }
}