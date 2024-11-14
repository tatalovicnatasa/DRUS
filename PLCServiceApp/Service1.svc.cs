using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace PLCServiceApp
{
    public class Service1 : IPub, ISub
    {

        delegate void VariableUpdateHandler(string nameOrAddress, int value); 
        static event VariableUpdateHandler VariableUpdateEvent;

        public static Dictionary<string, int> Variables = new Dictionary<string, int>{
            { "A1", 5 },
            { "A2", 6 },
            { "A3", 8 }
        };
        public void InitSub(string nameOrAddress)
        {

            if (int.TryParse(nameOrAddress, out int value)) // Ako je vrednost broj
            {
                var key = Variables.FirstOrDefault(kvp => kvp.Value == value).Key;

                if (!string.IsNullOrEmpty(key))
                {
                    VariableUpdateEvent += OperationContext.Current.GetCallbackChannel<ICallback>().VariableValueUpdated;
                    VariableUpdateEvent?.Invoke(key, value);
                }
                else
                {
                    OperationContext.Current.GetCallbackChannel<ICallback>().VariableValueUpdated(nameOrAddress, 0);
                }
            }
            else 
            {
                if (Variables.ContainsKey(nameOrAddress)) // kljuc 
                {
                    VariableUpdateEvent += OperationContext.Current.GetCallbackChannel<ICallback>().VariableValueUpdated;
                    VariableUpdateEvent?.Invoke(nameOrAddress, Variables[nameOrAddress]); // Obaveštavamo klijenta o trenutnoj vrednosti
                }
                else
                {
                    OperationContext.Current.GetCallbackChannel<ICallback>().VariableValueUpdated(nameOrAddress, 0); 

                }
            }
        }
        public void SendVariable(string message, int value) //salje novu varijablu
        {
            VariableUpdateEvent?.Invoke($"nova vrednost {message} je stigla {DateTime.Now}", value);
        }
    }
}