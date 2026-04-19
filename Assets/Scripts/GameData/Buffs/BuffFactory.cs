using System;
using System.Collections.Generic;
using System.Reflection;

namespace FenShen.GameData
{
    public static class BuffFactory
    {
        public static BuffBase Create(BuffSO data)
        {
            if (data == null)
            {
                return null;
            }

            BuffBase customBuff = TryCreateCustom(data.customRuntimeTypeName);
            if (customBuff != null)
            {
                return customBuff;
            }

            List<IBuff> parts = new List<IBuff>();

            if (data.modifiers != null && data.modifiers.Count > 0)
            {
                parts.Add(new StatBuff());
            }

            if ((data.periodicDamage > 0f || data.periodicHeal > 0f) && data.tickInterval > 0f)
            {
                parts.Add(new PeriodicBuff());
            }

            if (data.controlFlags != BuffControlFlag.None)
            {
                parts.Add(new ControlBuff());
            }

            if (parts.Count == 0)
            {
                return new EmptyBuff();
            }

            if (parts.Count == 1)
            {
                return parts[0] as BuffBase;
            }

            return new CompositeBuff(parts.ToArray());
        }

        private static BuffBase TryCreateCustom(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            Type runtimeType = Type.GetType(typeName, false);
            if (runtimeType == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    runtimeType = assemblies[i].GetType(typeName, false);
                    if (runtimeType != null)
                    {
                        break;
                    }
                }
            }

            if (runtimeType == null || !typeof(BuffBase).IsAssignableFrom(runtimeType))
            {
                return null;
            }

            return Activator.CreateInstance(runtimeType) as BuffBase;
        }

        private sealed class EmptyBuff : BuffBase
        {
        }
    }
}
