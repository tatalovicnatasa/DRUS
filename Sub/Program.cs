using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Sub
{
    // Subscriber : registruje se na servisu da bi primao poruke
    // Povezuje se WCF servisom i ceka dolazak novih poruka
    // Console Application 
    public class Callback : ServiceReference1.ISubCallback
    {
        public void VariableValueUpdated(string nameOrAddress, int value)
        {
            Console.WriteLine($"Vrednost promenljive {nameOrAddress}: {value}");
        }
    }
    internal class Program
    {
        static ServiceReference1.SubClient subClient;
        static void Main(string[] args)
        {
            Console.WriteLine("Unesite adresu ili naziv promenljive za pracenje (npr. 1001 ili 'Temperature'): ");
            string nameOrAddress = Console.ReadLine();

            InstanceContext ic = new InstanceContext(new Callback());
            subClient = new ServiceReference1.SubClient(ic);

            

            subClient.InitSub(nameOrAddress);
            Console.ReadLine();
        }
    }
}
