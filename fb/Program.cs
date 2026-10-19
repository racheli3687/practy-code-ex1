//fb bundle

using System.CommandLine;
using System.IO;
using System.Text;

var languageExtensions = new Dictionary<string, string[]>
{
    ["c#"] = [".cs"],
    ["javascript"] = [".js", ".jsx"],
    ["typescript"] = [".ts", ".tsx"],
    ["python"] = [".py"],
    ["java"] = [".java"]
};
var excludedDirectories = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
{
    "bin",
    "obj",
    ".git"
};


var bundleCommand = new Command("bundle", "bundle code files to a single file");
var createRspCommand = new Command("create-rsp", "create response files");

var languagesOption = new Option<string[]>("--languages")
{
    Description = "Programming languages to include",
    AllowMultipleArgumentsPerToken = true,
    Required = true

};
var outputOption = new Option<string>("--output")
{
    Description = "Output file name or full path",
    Required = true
};
var noteOption = new Option<bool>("--note")
{
    Description = "Should the source of the code be documented as a comment in the bundle file",
};
var sortOption = new Option<bool>("--sort")
{
    Description = "The copy order of the code files",
};
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines")
{
    Description = "Remove empty lines from the bundled file",
};

var authorOption = new Option<string>("--author")
{
    Description = "Write the author of the bundled code",
};




var rootCommand = new RootCommand("root command for file bundle CLI");



bundleCommand.SetAction((context) =>
{
    //מקבל את הנתיב הנוכחי של התיקייה שבה נמצא הקובץ fb.exe
    string currentDirectory = Directory.GetCurrentDirectory();


    //מקבל את הניתוב / שם הקובץ שהמשתמש הכניס בפקודה --output
    string? outputOptionValue = context.GetValue(outputOption);


    //בודק אם הנתיב שהמשתמש הכניס הוא נתיב מלא או רק שם קובץ, וממיר אותו לנתיב מלא בהתאם
    //string outputPath = Path.IsPathFullyQualified(outputOptionValue)
    //? Path.GetFullPath(outputOptionValue)
    //: Path.GetFullPath(
    //    Path.Combine(currentDirectory, outputOptionValue ?? "bundle.txt"));
    string outputPath = Path.IsPathFullyQualified(outputOptionValue ?? "")
    ? Path.GetFullPath(outputOptionValue ?? "bundle.txt")
    : Path.GetFullPath(
        Path.Combine(currentDirectory, outputOptionValue ?? "bundle.txt"));

    StringBuilder content = new StringBuilder();

    if (context.GetValue(authorOption) != null)
        content.AppendLine($"// Author: {context.GetValue(authorOption)}");

    string[] selectedLanguages = context.GetValue(languagesOption) ?? [];
    string[] languages = [];
    foreach(string lang in selectedLanguages)
    {
        string langLower = lang.ToLowerInvariant();
        if (langLower == "all")
        {
            languages = languageExtensions.Keys.ToArray();
            break;
        }
        if (!languageExtensions.ContainsKey(langLower))
        {
            Console.WriteLine($"Warning: Language '{lang}' is not supported and will be ignored.");
            
        }
        else
        {
            languages = languages.Append(langLower).ToArray();
        }
    }
    languages = languages.Distinct().ToArray();

    string[] fileExtensions = languages.SelectMany(lang => languageExtensions.GetValueOrDefault(lang.ToLower(), []))
        .Distinct()
        .ToArray();

    var allMatchFiles = EnumerateFilesSkippingDirectories(currentDirectory, outputPath)
        .Where(file => fileExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
    allMatchFiles = allMatchFiles.OrderBy(f => Path.GetFileName(f)).ToArray();
    if (context.GetValue(sortOption))
        allMatchFiles = allMatchFiles.OrderBy(f => Path.GetExtension(f)).ToArray();



    foreach (string file in allMatchFiles)
    {
        try
        {
            
            string fileContent = File.ReadAllText(file);

            if (context.GetValue(noteOption))
                content.AppendLine($"// File: {Path.GetFullPath(file)}");

            

            if(context.GetValue(removeEmptyLinesOption))
            {
                string[] lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                content.AppendLine(string.Join("\n", lines));
            }
            else
            {
                content.AppendLine(fileContent);
            }
            content.AppendLine();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file {file}: {ex.Message}");

        }
        
    }

    try
    {
        File.WriteAllText(outputPath, content.ToString());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error writing to output file {outputPath}: {ex.Message}");
        return;
    }
    Console.WriteLine("the sub command bundle ran successfully!");
});


createRspCommand.SetAction((context) =>
{

    string languagesValue =AskQuestion("Enter the languages to include (space-separated):", "all");


    string[] languagesArray = languagesValue.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
    string[] languageNewsArray = [];
    foreach (string lang in languagesArray)
    {
        string langLower = lang.ToLowerInvariant();
        if (langLower == "all")
        {
            languageNewsArray = languageExtensions.Keys.ToArray();
            break;
        }
        if (!languageExtensions.ContainsKey(langLower))
        {
            Console.WriteLine($"Warning: Language '{lang}' is not supported and will be ignored.");

        }
        else
        {
            languageNewsArray = languageNewsArray.Append(langLower).ToArray();
        }
    }
    languageNewsArray = languageNewsArray.Distinct().ToArray();
    if (languageNewsArray.Length == 0)
    {
        Console.WriteLine(
            "Error: No valid languages were selected.");
        return;
    }

    string outputValue = AskQuestion("Enter the output file name or full path:", "bundle.txt");
    while (true)
    {
        string fullOutputPath = Path.GetFullPath(outputValue);
        string? outputDirectory = Path.GetDirectoryName(fullOutputPath);

        if (outputDirectory != null && Directory.Exists(outputDirectory))
            break;

        Console.WriteLine(
            $"Error: Output directory '{outputDirectory}' does not exist.");

        outputValue = AskQuestion(
            "Enter the output file name or full path:",
            "bundle.txt");
    }
    bool noteValue = AskYesNoQuestion("Should the source of the code be documented as a comment in the bundle file? (y/n)");
    bool sortValue = AskYesNoQuestion("Should the copy order of the code files be sorted? (y/n)");
    bool removeEmptyLinesValue = AskYesNoQuestion("Should empty lines be removed from the bundled file? (y/n)");
    bool authorValue = AskYesNoQuestion("Do you want to include the author of the bundled code? (y/n)");


    var responseLines = new List<string>
    {
        "bundle",
        "--languages"
    };
    
    responseLines.AddRange(languageNewsArray);
    responseLines.Add("--output");
    responseLines.Add($"\"{outputValue.Trim()}\"");
    if (noteValue)
    {
        responseLines.Add("--note");
    }
    if (sortValue)
    {
        responseLines.Add("--sort");
    }
    if (removeEmptyLinesValue)
    {
        responseLines.Add("--remove-empty-lines");
    }
    if (authorValue)
    {
        string authorName = AskQuestion("Enter the author name:", "Unknown");
        responseLines.Add("--author");
        responseLines.Add($"\"{authorName}\"");
    }

    string path = Path.Combine(Directory.GetCurrentDirectory(), "bundle.rsp");
    try
    {
        File.WriteAllLines(path, responseLines);
        Console.WriteLine($"Response file created successfully: {path}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error writing to response file {path}: {ex.Message}");
        return;
    }
});



IEnumerable<string> EnumerateFilesSkippingDirectories(
    string directory,string thisFile)
{
    // שלב 1: מחזירים את הקבצים שבתיקייה הנוכחית
    foreach (string file in Directory.EnumerateFiles(directory))
    {
        if (!string.Equals(
         Path.GetFullPath(file),
         Path.GetFullPath(thisFile),
         StringComparison.OrdinalIgnoreCase))
        {
            yield return file;
        }
    }

    // שלב 2: עוברים על תתי-התיקיות
    foreach (string subdirectory in
             Directory.EnumerateDirectories(directory))
    {
        string directoryName = Path.GetFileName(subdirectory);

        // שלב 3: מדלגים על תיקיות מוחרגות
        if (excludedDirectories.Contains(directoryName))
        {
            continue;
        }

        // שלב 4: סורקים רקורסיבית תיקיות שאינן מוחרגות
        foreach (string file in
                 EnumerateFilesSkippingDirectories(subdirectory, thisFile))
        {
            yield return file;
        }
    }
}

string AskQuestion(string question,string defaultValue)
{
    Console.WriteLine($"{question} [{defaultValue}]: ");
    string? input = Console.ReadLine();
    return string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();
}
bool AskYesNoQuestion(string question)
{
    Console.WriteLine($"{question} (y/n): ");
    string? input = Console.ReadLine();
    input=input?.Trim();

    while (true)
    {
        if(string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        else if (string.Equals(input, "n", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        else
        {
            Console.WriteLine("Invalid input. Please enter 'y' or 'n': ");
            input = Console.ReadLine();
        }
    }

}




// Add the bundle command to the root command and add options to the bundle command
rootCommand.Add(bundleCommand);
rootCommand.Add(createRspCommand);
bundleCommand.Options.Add(languagesOption);
bundleCommand.Options.Add(outputOption);
bundleCommand.Options.Add(noteOption);
bundleCommand.Options.Add(sortOption);
bundleCommand.Options.Add(removeEmptyLinesOption);
bundleCommand.Options.Add(authorOption);


await rootCommand.Parse(args).InvokeAsync();

