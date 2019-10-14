using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumRcw
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("EnumRcw プロセス名");
                return;
            }
            Console.WriteLine(args[0]);
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(args[0]))
            {
                int pid = process.Id;
                Console.WriteLine("{0} {1} =======================================================", args[0], pid);
                using (var dataTarget = DataTarget.AttachToProcess(pid, 1000))
                {
                    Console.WriteLine(dataTarget.Architecture);
                    var clrVersion = dataTarget.ClrVersions.First();
                    var dacInfo = clrVersion.DacInfo;
                    ClrRuntime runtime = clrVersion.CreateRuntime();
                    foreach (var obj in runtime.Heap.EnumerateObjects())
                    {
                        ClrType type = obj.Type;
                        ulong size = obj.Size;
                        if (type.IsRCW(obj))
                        {
                            RcwData rcw = type.GetRCWData(obj);
                            if (rcw != null)
                            {
                                string ifname = "";
                                foreach (var i in rcw.Interfaces)
                                {
                                    ifname += i.Type.Name + ",";
                                }
                                Console.WriteLine("{0,16:X} {1,12:n0} {2} {3} {4} {5}", obj.Address, size, type.Name, rcw.RefCount, rcw.Disconnected, ifname);

                            }
                            else
                            {
                                Console.WriteLine("{0,16:X} {1,12:n0} {2} (GetRCWDataに失敗)", obj.Address, size, type.Name);

                            }
                        }
                    }
                }
            }
        }
    }
}
