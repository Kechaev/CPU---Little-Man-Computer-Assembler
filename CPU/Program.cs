using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CPU
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            // Creation of machine m
            Machine m = new Machine(99);
            string[] assemblyCode = new string[]
            {
                // Assembly Code
                "INP",
                "STA 91",
                "INP",
                "STA 92",
                "loop LDA 94",
                "ADD 91",
                "STA 94",
                "LDA 92",
                "SUB 93",
                "STA 92",
                "BRP loop",
                "LDA 94",
                "SUB 91",
                "STA 94",
                "OUT",
                "HLT"
            };
            m.Assemble(assemblyCode);
            // Allocate values to memory
            m.SetMemory(93, 1);
            m.SetMemory(94, 0);
            // End Allocation
            m.DisplayMemory();
            Console.Write("\n\nRun? ");
            Console.ReadLine();
            Console.WriteLine();
            m.Start();
            m.DisplayMemory();

            Console.ReadLine();
        }
    }

    class Machine
    {
        private int[] memory;
        private int PC = 0, MAR = 0, MDR = 0, CIR = 0, ACC = 0;
        private string CUopcode;
        private int[] registers;
        private bool isRunning = false;
        private string[] assemblyCode;
        private Dictionary<string, int> instructionSet = new Dictionary<string, int>
        {
            {"HLT", 0 },
            {"ADD", 1 },
            {"SUB", 2 },
            {"STA", 3 },
            {"LDA", 5 },
            {"BRA", 6 },
            {"BRZ", 7 },
            {"BRP", 8 },
            {"INP", 901 },
            {"OUT", 902 }
        };

        public Machine(int memorySize)
        {
            memory = new int[memorySize];
        }

        public void SetMemory(int address, int value)
        {
            memory[address] = value;
        }

        public void Start()
        {
            isRunning = true;
            while (isRunning)
            {
                FetchDecodeExecute();
            }
        }

        private void FetchDecodeExecute()
        {
            Fetch();
            Decode();
            Execute();
        }

        private void Fetch()
        {
            MAR = PC;
            PC++;
            MDR = memory[MAR];
            CIR = MDR;
        }

        private void Decode()
        {
            int opcode = (CIR - CIR % 100) / 100;
            foreach (var v in instructionSet)
            {
                if (v.Value == opcode)
                {
                    CUopcode = v.Key;
                }
            }
            if (CIR == 901)
            {
                CUopcode = "INP";
            }
            else if (CIR == 902)
            {
                CUopcode = "OUT";
            }
        }

        private void Execute()
        {
            if (CIR % 100 != 0)
            {
                MAR = CIR % 100;
                MDR = memory[MAR];
            }
            switch (CUopcode)
            {
                case "HLT":
                    isRunning = false;
                    //Console.WriteLine("HALTED PROGRAM");
                    break;
                case "ADD":
                    ACC += MDR;
                    //Console.WriteLine($"INCREMENTED ACCUMULATOR BY {MDR}");
                    break;
                case "SUB":
                    ACC -= MDR;
                    //Console.WriteLine($"DECREMENTED ACCUMULATOR BY {MDR}");
                    break;
                case "STA":
                    memory[MAR] = ACC;
                    //Console.WriteLine($"MEMORY ADDRESS [{MAR}] SET TO {ACC}");
                    break;
                case "LDA":
                    ACC = MDR;
                    //Console.WriteLine($"LOADED {MDR} INTO THE ACCUMULATOR");
                    break;
                case "BRA":
                    PC = MAR;
                    //Console.WriteLine($"BRANCHED TO LINE {MAR}");
                    break;
                case "BRZ":
                    if (ACC == 0)
                    {
                        PC = MAR;
                    }
                    //Console.WriteLine($"BRANCHED TO LINE {MAR}");
                    break;
                case "BRP":
                    if (ACC >= 0)
                    {
                        PC = MAR;
                    }
                    //Console.WriteLine($"BRANCHED TO LINE {MAR}");
                    break;
                case "INP":
                    Console.Write("Input: ");
                    string input = Console.ReadLine();
                    Console.WriteLine();
                    int intInput;
                    try
                    {
                        intInput = Convert.ToInt32(input);
                    }
                    catch (Exception)
                    {
                        intInput = 0;
                    }
                    ACC = intInput;
                    break;
                case "OUT":
                    Console.WriteLine($"Output: {ACC}");
                    Console.WriteLine();
                    break;
                default:
                    throw new Exception("Unkown opcode");
            }
        }

        public void DisplayRegisters()
        {
            Console.WriteLine("Registers");
            Console.WriteLine($"PC: {PC}\nMAR: {MAR}\nMDR: {MDR}\nCIR: {CIR}\nACC: {ACC}");
        }

        private string RemoveLabelFromBeginning(string line, string label)
        {
            int len = label.Length + 1;
            return line.Substring(len, line.Length - len);
        }

        private int FindLabelIndex(string[] assemblyCode, string label)
        {
            int count = 0;
            foreach (string line in assemblyCode)
            {
                string[] parts = line.Split(' ');
                if (parts[0] == label)
                {
                    return count;
                }
                count++;
            }
            return -1;
        }

        public void Assemble(string[] assemblyCode)
        {
            this.assemblyCode = assemblyCode;
            List<int> machineCode = new List<int>();

            int count = 0;

            foreach (string line in assemblyCode)
            {
                count++;
                Console.WriteLine($"{count}. {line}");
            }

            count = 0;
            string label = ScanLabel(assemblyCode);

            foreach (string l in assemblyCode)
            {
                string line = l;
                line.Trim();

                string[] parts = line.Split(' ');
                int opcode = -1;
                if (instructionSet.ContainsKey(parts[0]))
                {
                    opcode = instructionSet[parts[0]];
                }
                else
                {
                    // Remove label temporarily
                    line = RemoveLabelFromBeginning(line, label);
                    parts = line.Split(' ');
                    opcode = instructionSet[parts[0]];
                }
                int operand = 0;
                if (parts.Count() > 1)
                {
                    if (parts[0][0] == 'B')
                    {
                        operand = FindLabelIndex(assemblyCode, parts[1]);
                        assemblyCode[operand] = RemoveLabelFromBeginning(assemblyCode[operand], parts[1]);
                    }
                    else
                    {
                        operand = Convert.ToInt32(parts[1]);
                    }
                }
                if (opcode > 900)
                {
                    machineCode.Add(opcode);
                }
                else
                {
                    machineCode.Add(opcode * 100 + operand);
                }
            }

            for (int i = 0; i < machineCode.Count; i++)
            {
                memory[i] = machineCode[i];
            }
        }

        private string ScanLabel(string[] assemblyCode)
        {
            // Only finds the first label, could use a stack to search for all
            foreach (string line in assemblyCode)
            {
                string[] parts = line.Split(' ');
                if (!instructionSet.ContainsKey(parts[0]))
                {
                    return parts[0];
                }
            }
            return null;
        }

        public void DisplayMemory()
        {
            int count = 0;
            foreach (int b in memory)
            {
                Console.Write($"0x{DecToHex(count).PadLeft(2, '0')}: {b} \t");
                count++;
                if (count % 8 == 0)
                {
                    Console.Write("\n");
                }
            }
        }

        private string DecToHex(int dec)
        {
            Stack<int> ints = new Stack<int>();
            while (dec > 0)
            {
                ints.Push(dec % 16);
                dec /= 16;
            }

            string hex = "";

            while (ints.Count > 0)
            {
                int c = ints.Pop();
                if (c < 10)
                {
                    hex += c.ToString();
                }
                else
                {
                    c -= 10;
                    hex += Convert.ToChar(c + 'A');
                }
            }

            return hex;
        }
    }
}
