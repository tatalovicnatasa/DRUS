using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace PLCServiceApp
{
    // Servis : generise i salje vrednosti 
    public class Service1 : IPub, ISub
    {

        // tip funkcije za obavestenje 
        delegate void VariableUpdateHandler(string nameOrAddress, int value); // definise tip funkcije koja prima poruku
        static event VariableUpdateHandler VariableUpdateEvent;// obavestava sve subscribere kada stigne 

        public static Dictionary<string, string> Variables = new Dictionary<string, string>{
            { "A1", "V1" },  // Primer promenljive sa adresom A1
            { "A2", "V2" },
            { "A3", "V3" }
        };

        //cuva korisnike sa njihovim podacima
        public static Dictionary<string, ICallback> HMIClients = new Dictionary<string, ICallback>(); // dodaje subscribere


        // Registracija putem WCF servisa klijenata i pracenje promenljive
        public void InitSub(string nameOrAddress)
        {
            // Provera da li je unesena adresa ili naziv promenljive koja postoji u rečniku
            if (Variables.ContainsKey(nameOrAddress) || Variables.ContainsValue(nameOrAddress))
            {
                // Ako promenljiva postoji, registrovanje HMI klijenta
                if (!HMIClients.ContainsKey(nameOrAddress)) // ako nema 
                {
                    HMIClients.Add(nameOrAddress, OperationContext.Current.GetCallbackChannel<ICallback>());

                    // Povezujemo callback metod
                    VariableUpdateEvent += OperationContext.Current.GetCallbackChannel<ICallback>().VariableValueUpdated;

                    //Console.WriteLine($"HMI sa nazivom ili adresom {nameOrAddress} je uspešno registrovan.");
                }
                else
                {
                    //klijent postoji 
                    // Console.WriteLine($"HMI sa nazivom ili adresom {nameOrAddress} već postoji.");
                }
            }
            else
            {
                // Ako promenljiva ne postoji, vraćamo obaveštenje o grešci
                Console.WriteLine($"Greška: Promenljiva sa nazivom ili adresom {nameOrAddress} ne postoji u PLC-u.");
                OperationContext.Current.GetCallbackChannel<ICallback>().VariableValueUpdated(nameOrAddress, 0); // Vraća nulu

            } // nula 
        }
        public void SendVariable(string message, int value) //salje novu varijablu
        {
            Console.WriteLine($"Vrednost promenljive {message} = {value}");
            VariableUpdateEvent?.Invoke(message, value); 


        }
    }
}

