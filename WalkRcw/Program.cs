using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkHeap
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("EnumRcw プロセス名 オブジェクトのアドレス（16進数)");
                return;
            }

            string name = args[0];
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(args[0]))
            {
                int pid = process.Id;
                Console.WriteLine("{0} {1} =======================================================", args[0], pid);
                ulong taregetPtr = ulong.Parse(args[1], System.Globalization.NumberStyles.HexNumber);

                using (var dataTarget = DataTarget.AttachToProcess(pid, 1000))
                {
                    var clrVersion = dataTarget.ClrVersions.First();
                    var dacInfo = clrVersion.DacInfo;
                    ClrRuntime runtime = clrVersion.CreateRuntime();
                    var stack = new Stack<ulong>();

                    var heap = runtime.Heap;
                    if (heap.CanWalkHeap)
                    {
                        Console.WriteLine("-----");
                        foreach (var ptr in heap.EnumerateObjectAddresses())
                        {
                            var type = heap.GetObjectType(ptr);
                            if ( taregetPtr != ptr)
                            {
                                continue;
                            }
                            Console.WriteLine("find");

                            // todo: retention path
                            Console.WriteLine("roots...");
                            foreach (var root in heap.EnumerateRoots())
                            {
                                stack.Clear();
                                stack.Push(root.Object);

                                if (GetPathToObject(heap, ptr, stack, new HashSet<ulong>()))
                                {
                                    // Print retention path
                                    var depth = 0;
                                    foreach (var address in stack)
                                    {
                                        var t = heap.GetObjectType(address);
                                        if (t == null)
                                        {
                                            Console.WriteLine("{0} {1,16:X} ", new string('+', depth++), address);
                                            continue;
                                        }

                                        Console.WriteLine("{0} {1,16:X} - {2} - {3} bytes", new string('+', depth++), address, t.Name, t.GetSize(address));
                                    }

                                    break;
                                }
                            }
                            Console.WriteLine("Finalize");
                            foreach (var obj in runtime.EnumerateFinalizerQueueObjectAddresses())
                            {
                                Console.WriteLine(" {0,16:X}", obj);
                                stack.Clear();
                                stack.Push(obj);

                                if (GetPathToObject(heap, ptr, stack, new HashSet<ulong>()))
                                {
                                    // Print retention path
                                    var depth = 0;
                                    foreach (var address in stack)
                                    {
                                        var t = heap.GetObjectType(address);
                                        if (t == null)
                                        {
                                            Console.WriteLine("{0} {1,16:X} ", new string('+', depth++), address);
                                            continue;
                                        }

                                        Console.WriteLine("{0} {1,16:X} - {2} - {3} bytes", new string('+', depth++), address, t.Name, t.GetSize(address));
                                    }

                                    break;
                                }
                            }
                            break;
                        }
                    }

                }

            }
        }

        // https://blog.maartenballiauw.be/post/2017/01/03/exploring-.net-managed-heap-with-clrmd.html
        private static bool GetPathToObject(ClrHeap heap, ulong objectPointer, Stack<ulong> stack, HashSet<ulong> touchedObjects)
        {
            // Start of the journey - get address of the first objetc on our reference chain
            var currentObject = stack.Peek();

            // Have we checked this object before?
            if (!touchedObjects.Add(currentObject))
            {
                return false;
            }

            // Did we find our object? Then we have the path!
            if (currentObject == objectPointer)
            {
                return true;
            }


            // Enumerate internal references of the object
            var found = false;
            var type = heap.GetObjectType(currentObject);
            if (type != null)
            {
                type.EnumerateRefsOfObject(currentObject, (innerObject, fieldOffset) =>
                {
                    if (innerObject == 0 || touchedObjects.Contains(innerObject))
                    {
                        return;
                    }

                    // Push the object onto our stack
                    stack.Push(innerObject);
                    if (GetPathToObject(heap, objectPointer, stack, touchedObjects))
                    {
                        found = true;
                        return;
                    }

                    // If not found, pop the object from our stack as this is not the tree we're looking for
                    stack.Pop();
                });
            }

            return found;
        }
    }
}
