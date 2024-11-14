using Pub.ServiceReference1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pub
{
    internal class Program
    {
        static ServiceReference1.PubClient pubClient = new ServiceReference1.PubClient(); // objekat klijent koji se povezuje sa WCF servisom 
        static void Main(string[] args)
        {
            while (true) 
            {
                Thread.Sleep(3000);

                pubClient.SendVariable("kkk", new Random().Next(0, 100)); 
            }

        }
    }
}
