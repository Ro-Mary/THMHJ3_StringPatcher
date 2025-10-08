using System;
using System.IO;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Text;
using System.Linq;

class MappingEntry {
    public string Orig;
    public string Repl;
    public HashSet<string> NextOpcodes;
}

class Program {

    static string? FindFileUpwards(string startDir, string fileName, int maxLevels = 8) {
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i <= maxLevels && dir != null; i++) {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    static int Main(string[] args) {
        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var exeDir = Path.GetDirectoryName(exePath);
        if (exeDir == null) exeDir = Directory.GetCurrentDirectory();

        //var inputPath = Path.Combine(exeDir, "../THMHJ.exe");
        //var mappingCsv = Path.Combine(exeDir, "mapping.csv");
        //var outputPath = Path.Combine(exeDir, "THMHJ.exe");

        string? inputPath = FindFileUpwards(exeDir, "THMHJ.exe");
        if (inputPath == null) {
            Console.WriteLine("THMHJ.exe를 찾을 수 없습니다.");
            WaitExit();
            return 2;
        }

        string? mappingCsv = FindFileUpwards(exeDir, "mapping.csv");
        if (mappingCsv == null) {
            Console.WriteLine("mapping.csv를 찾을 수 없습니다.");
            WaitExit();
            return 3;
        }

        Console.WriteLine($"Input: {inputPath}");
        Console.WriteLine($"Mapping: {mappingCsv}");

        List<MappingEntry> map = LoadMapping(mappingCsv);
        if (map.Count == 0) {
            Console.WriteLine("mapping.csv 파일이 비어있습니다.");
            WaitExit();
            return 4;
        }

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(exeDir);

        var readerParams = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false, ReadSymbols = false };
        AssemblyDefinition assembly;
        try {
            assembly = AssemblyDefinition.ReadAssembly(inputPath, readerParams);
        } 
        catch (Exception ex) {
            Console.WriteLine("어셈블리 읽기 실패:" + ex.Message);
            WaitExit();
            return 5;
        }

        int replaced = 0;
        var logSb = new StringBuilder();
        logSb.AppendLine($"StringPatcher log - {DateTime.Now}");
        logSb.AppendLine($"Input: {inputPath}");
        logSb.AppendLine($"Mapping: {mappingCsv}");
        logSb.AppendLine();

        try {
            var originalDir = Path.GetDirectoryName(mappingCsv)!;
            var backupDir = Path.Combine(originalDir, "backup");

            Directory.CreateDirectory(backupDir);

            var fileName = Path.GetFileNameWithoutExtension(inputPath) + "_bak.exe";
            var bak = Path.Combine(backupDir, fileName);

            File.Copy(inputPath, bak, overwrite: true);
            Console.WriteLine($"백업 성공: {bak}");
        }
        catch (Exception ex) {
            Console.WriteLine("백업 실패: " + ex.Message);
        }

        foreach (var module in assembly.Modules)
            foreach (var type in module.Types)
                replaced += ProcessType(type, map, logSb);

        try {
            string outputPath = Path.Combine(Path.GetDirectoryName(mappingCsv), "publish/", Path.GetFileName(inputPath));

            assembly.Write(outputPath);

            Console.WriteLine("패치된 파일 생성 완료: " + outputPath);
            logSb.AppendLine("패치 파일 경로: " + outputPath);
            logSb.AppendLine("교체된 항목 수: " + replaced);

            File.WriteAllText(Path.Combine(exeDir, "patch_log.txt"), logSb.ToString(), Encoding.UTF8);
            Console.WriteLine("수정 로그: patch_log.txt");
        }
        catch (Exception ex) {
            Console.WriteLine("패치 파일 생성 실패: " + ex.Message);
            logSb.AppendLine("쓰기 실패: " + ex.Message);
            File.WriteAllText(Path.Combine(exeDir, "patch_log.txt"), logSb.ToString(), Encoding.UTF8);
            WaitExit();
            return 6;
        }
        WaitExit();
        return 0;
    }

    static void WaitExit() {
        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }

    static List<MappingEntry> LoadMapping(string csvPath) {
        var list = new List<MappingEntry>();
        foreach (var rawLine in File.ReadAllLines(csvPath, Encoding.UTF8)) {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var parts = ParseCsvLine(line);
            if (parts.Length < 2) continue;
            list.Add(new MappingEntry { Orig = parts[0], Repl = parts[1] });
        }
        return list;
    }

    static string[] ParseCsvLine(string line) {
        var res = new List<string>();
        bool inQuote = false;
        var cur = new StringBuilder();
        for (int i = 0; i < line.Length; i++) {
            char c = line[i];
            if (c == '"') { inQuote = !inQuote; continue; }
            if (c == ',' && !inQuote) { res.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(c);
        }
        res.Add(cur.ToString());
        return res.ToArray();
    }

    static int ProcessType(TypeDefinition type, List<MappingEntry> map, StringBuilder logSb) {
        int replaced = 0;
        foreach (var method in type.Methods) {
            if (!method.HasBody) continue;
            var instrs = method.Body.Instructions;
            for (int i = 0; i < instrs.Count; i++) {
                var ins = instrs[i];
                if (ins.OpCode == OpCodes.Ldstr) {
                    var s = ins.Operand as string;
                    if (s == null) continue;
                    foreach (var m in map) {
                        if (s == m.Orig) {
                            ins.Operand = m.Repl;
                            replaced++;
                            var ln = $"교체: {method.FullName}: \"{m.Orig}\" -> \"{m.Repl}\"";
                            Console.WriteLine(ln);
                            logSb.AppendLine(ln);
                            break;
                        }
                    }
                }
            }
        }
        foreach (var nested in type.NestedTypes)
            replaced += ProcessType(nested, map, logSb);
        return replaced;
    }
}

