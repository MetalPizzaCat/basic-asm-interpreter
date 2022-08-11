using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class SimpleAssembler
{
    public static string? Interpret(string program)
    {
        Dictionary<string, int> registers = new Dictionary<string, int>();
        //pre-generated map of jumpo labels
        Dictionary<string, int> labels = new Dictionary<string, int>();
        //simple helper function for parsing arguments
        int GetValue(string data) { int value; return (int.TryParse(data, out value) ? value : registers[data]); }
        //this will match any ; symbol and capture everything after it till the end of the line
        Regex commentRegEx = new Regex(@"(\ )*?(;)(.*)");
        //matches any text that has : at the end
        Regex labelRegEx = new Regex(@"([A-z])\w+(?=:)");
        //this catches anything that is of 'text' form 
        Regex literalRegEx = new Regex(@"(?=')(.*)(?<=')");

        //('.*?'|[^',\s]+)(?=\s*,|\s*$)
        /*
            (?=\s*,|\s*$) 
            ?=\s*,          this matches comma with any amount of spaces before it
            \s*$            this matches end of the string with any amount of spaces before it

            '.*?'           this extracts quoted text
            [^',\s]+        this will extract any other text that doesn't have quotation marks 

        */
        Regex msgRegEx = new Regex(@"('.*?'|[^',\s]+)(?=\s*,|\s*$)");

        string? result = null;
        //result of last cmp operation
        //0 - values are equal
        //1 - a < b
        //2 - a > b
        int cmp = 0;
        int pointer = 0;
        //Used for jumping out from subroutines
        Stack<int> stack = new Stack<int>();
        //This is cleaned up list of all operations that will be executed
        List<string> operations = new List<string>();

        //preparation
        //Clean up input to remove comments and empty lines
        program.Split("\n").ToList().ForEach(p =>
        {
            string op = commentRegEx.Replace(p, "").Trim();
            if (op != string.Empty)
            {
                operations.Add(op);
            }
        });
        //find and record all labels
        for (int i = 0; i < operations.Count; i++)
        {
            Match match = labelRegEx.Match(operations[i]);
            if (match.Value != string.Empty)
            {
                labels.Add(match.Value, i);
            }
        }

        while (pointer < operations.Count && pointer >= 0)
        {
            string[] input = operations[pointer].Split(new char[] { ',', ' ' }).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            switch (input[0])
            {
                case "mov":
                    {
                        if (!registers.ContainsKey(input[1]))
                        {
                            registers.Add(input[1], GetValue(input[2]));
                        }
                        else
                        {
                            registers[input[1]] = GetValue(input[2]);
                        }
                    }
                    break;
                case "inc":
                    registers[input[1]]++;
                    break;
                case "dec":
                    registers[input[1]]--;
                    break;
                case "add":
                    registers[input[1]] += GetValue(input[2]);
                    break;
                case "sub":
                    registers[input[1]] -= GetValue(input[2]);
                    break;
                case "mul":
                    registers[input[1]] *= GetValue(input[2]);
                    break;
                case "div":
                    registers[input[1]] /= GetValue(input[2]);
                    break;
                case "cmp":
                    cmp = GetValue(input[1]) == GetValue(input[2]) ?
                        0 :
                        (GetValue(input[1]) < GetValue(input[2]) ? 1 : 2);
                    break;
                case "jmp":
                    pointer = labels[input[1]];
                    break;
                case "jne":
                    if (cmp != 0)
                    {
                        pointer = labels[input[1]];
                    }
                    break;
                case "je":
                    if (cmp == 0)
                    {
                        pointer = labels[input[1]];
                    }
                    break;
                case "jge":
                    if (cmp == 0 || cmp == 2)
                    {
                        pointer = labels[input[1]];
                    }
                    break;
                case "jg":
                    if (cmp == 2)
                    {
                        pointer = labels[input[1]];
                    }
                    break;
                case "jle":
                    if (cmp == 1 || cmp == 0)
                    {
                        pointer = labels[input[1]];
                    }
                    break;
                case "jl":
                    if (cmp == 1)
                    {
                        pointer = labels[input[1]];
                    }
                    break;
                case "call":
                    stack.Push(pointer);
                    pointer = labels[input[1]];
                    break;
                case "ret":
                    pointer = stack.Pop();
                    break;
                case "end":
                    return result;
                case "msg":
                    string op = operations[pointer].Substring(3).Trim();
                    MatchCollection matches = msgRegEx.Matches(op);
                    result = string.Empty;
                    foreach (Match match in matches)
                    {
                        result += literalRegEx.IsMatch(match.Value) ? match.Value.Replace("\'", string.Empty) : GetValue(match.Value);
                    }
                    break;
            }
            
            pointer++;
        }
        return null;
    }

    private static string[] progs = {
            "\n; My first program\nmov  a, 5\ninc  a\ncall function\nmsg  '(5+1)/2 = ', a    ; output message\nend\n\nfunction:\n    div  a, 2\n    ret\n",
            "\nmov   a, 5\nmov   b, a\nmov   c, a\ncall  proc_fact\ncall  print\nend\n\nproc_fact:\n    dec   b\n    mul   c, b\n    cmp   b, 1\n    jne   proc_fact\n    ret\n\nprint:\n    msg   a, '! = ', c ; output text\n    ret\n",
            "\nmov   a, 8            ; value\nmov   b, 0            ; next\nmov   c, 0            ; counter\nmov   d, 0            ; first\nmov   e, 1            ; second\ncall  proc_fib\ncall  print\nend\n\nproc_fib:\n    cmp   c, 2\n    jl    func_0\n    mov   b, d\n    add   b, e\n    mov   d, e\n    mov   e, b\n    inc   c\n    cmp   c, a\n    jle   proc_fib\n    ret\n\nfunc_0:\n    mov   b, c\n    inc   c\n    jmp   proc_fib\n\nprint:\n    msg   'Term ', a, ' of Fibonacci series is: ', b        ; output text\n    ret\n",
            "\nmov   a, 11           ; value1\nmov   b, 3            ; value2\ncall  mod_func\nmsg   'mod(', a, ', ', b, ') = ', d        ; output\nend\n\n; Mod function\nmod_func:\n    mov   c, a        ; temp1\n    div   c, b\n    mul   c, b\n    mov   d, a        ; temp2\n    sub   d, c\n    ret\n",
            "\nmov   a, 81         ; value1\nmov   b, 153        ; value2\ncall  init\ncall  proc_gcd\ncall  print\nend\n\nproc_gcd:\n    cmp   c, d\n    jne   loop\n    ret\n\nloop:\n    cmp   c, d\n    jg    a_bigger\n    jmp   b_bigger\n\na_bigger:\n    sub   c, d\n    jmp   proc_gcd\n\nb_bigger:\n    sub   d, c\n    jmp   proc_gcd\n\ninit:\n    cmp   a, 0\n    jl    a_abs\n    cmp   b, 0\n    jl    b_abs\n    mov   c, a            ; temp1\n    mov   d, b            ; temp2\n    ret\n\na_abs:\n    mul   a, -1\n    jmp   init\n\nb_abs:\n    mul   b, -1\n    jmp   init\n\nprint:\n    msg   'gcd(', a, ', ', b, ') = ', c\n    ret\n",
            "\ncall  func1\ncall  print\nend\n\nfunc1:\n    call  func2\n    ret\n\nfunc2:\n    ret\n\nprint:\n    msg 'This program should return null'\n",
            "\nmov   a, 2            ; value1\nmov   b, 10           ; value2\nmov   c, a            ; temp1\nmov   d, b            ; temp2\ncall  proc_func\ncall  print\nend\n\nproc_func:\n    cmp   d, 1\n    je    continue\n    mul   c, a\n    dec   d\n    call  proc_func\n\ncontinue:\n    ret\n\nprint:\n    msg a, '^', b, ' = ', c\n    ret\n"};

    private static string[] expected = {"(5+1)/2 = 3",
                                        "5! = 120",
                                        "Term 8 of Fibonacci series is: 21",
                                        "mod(11, 3) = 2",
                                        "gcd(81, 153) = 9",
                                        null,
                                        "2^10 = 1024"};

    public static void Main(string[] args)
    {
        /*for (int i = 0; i < expected.Length; i++)
        {
            System.Console.WriteLine($"Executing {progs[i]}");
            System.Console.WriteLine($"{Interpret(progs[i])}");
            System.Console.WriteLine();
            System.Console.WriteLine();
        }*/
        System.Console.WriteLine(progs[5]);
        System.Console.WriteLine($"{Interpret(progs[5])}");

    }
}
