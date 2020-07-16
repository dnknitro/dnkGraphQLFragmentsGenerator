using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace dnkGraphQLFragmentsGenerator
{
    internal class Program
    {
        /// <summary>
        /// Extracts GraphQL Fragments from `--schema-file` using `--fragment-regex` regular expression and saves those into `--output-folder`
        /// </summary>
        /// <param name="schemaFile">Path to GraphQL schema file</param>
        /// <param name="outputFolder">Path to base output folder</param>
        /// <param name="operationNames">List of queries and mutations to generate</param>
        /// <param name="verbose">Turns on verbose logging</param>
        /// <param name="fragmentRegex">Regular expression to find fragments in schema file</param>
        /// <param name="fragmentNameRegex">Regular expression to extract fragment name from --fragment-regex</param>
        private static void Main(string schemaFile = "schema.graphql", string outputFolder = @"src/generated", string operationNames = "", bool verbose = false,
            string fragmentRegex = @"^type \w+Gql \{.+?\}$", 
            string fragmentNameRegex = @"^type (?:(\w+)Gql).*$"
            )
        {
            var operationNamesList = operationNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToUpper().Trim()).ToArray();

            ConsoleWrite("Reading graphql schema from ");
            ConsoleWrite(schemaFile, InfoColor, true);

            var schema = File.ReadAllText(schemaFile);

            var matches = Regex.Matches(schema, fragmentRegex, RegexOptions.Singleline | RegexOptions.Multiline);

            var fragments = matches.Select(match =>
            (
                Regex.Replace(match.Value, fragmentNameRegex, "$1", RegexOptions.Singleline),
                match.Value
            )).ToList();

            var fragmentsFolder = Path.Combine(outputFolder, "fragments");
            var mutationsFolder = Path.Combine(outputFolder, "mutations");
            var queriesFolder = Path.Combine(outputFolder, "queries");

            Directory.CreateDirectory(fragmentsFolder);
            Directory.CreateDirectory(mutationsFolder);
            Directory.CreateDirectory(queriesFolder);

            var mutationsCount = 0;
            var queriesCount = 0;
            foreach (var (fragmentName, fragmentContents) in fragments)
            {
                if (verbose)
                {
                    ConsoleWrite("fragment ");
                    ConsoleWrite(fragmentName, GoodColor, true);
                }

                var fragment = GetFragment(fragmentContents);
                var fragmentFile = Path.Combine(fragmentsFolder, $"{fragmentName}Fragment.graphql");
                File.WriteAllText(fragmentFile, fragment);


                var mutationFile = Path.Combine(mutationsFolder, $"Save{fragmentName}Mutation.graphql");
                if (operationNamesList.Contains(MutationOperationName(fragmentName).ToUpper()))
                {
                    mutationsCount++;
                    File.WriteAllText(mutationFile, GetMutation(fragmentName));
                }
                else
                {
                    File.Delete(mutationFile);
                }


                var queryFile = Path.Combine(queriesFolder, $"{fragmentName}Query.graphql");
                if (operationNamesList.Contains(QueryOperationName(fragmentName).ToUpper()))
                {
                    queriesCount++;
                    File.WriteAllText(queryFile, GetQuery(fragmentName));
                }
                else
                {
                    File.Delete(queryFile);
                }
            }

            WriteSummary(fragments.Count, "fragments", fragmentsFolder);
            WriteSummary(mutationsCount, "mutations", mutationsFolder);
            WriteSummary(queriesCount, "queries", queriesFolder);

            // var all = string.Join(Environment.NewLine + Environment.NewLine, fragments.Select(x => x.Value));
            //File.WriteAllText(@"all.graphql", all);
            //Console.WriteLine(all);
        }

        private static void WriteSummary(int count, string name, string location)
        {
            ConsoleWrite("Saved ");
            ConsoleWrite(count, GoodColor);
            ConsoleWrite($" {name} to ");
            ConsoleWrite(location, InfoColor, true);
        }

        private static string CamelCaseEntityName(string entityName) => char.ToLower(entityName[0]) + entityName.Substring(1);

        private static string GetFragment(string fragmentContents)
        {
            var fragment = Regex.Replace(fragmentContents, @"^type ((\w+)Gql)", "fragment $2 on $1")
                .Replace("!", "");

            var subFragmentRegex = @": \[?(\w+?)Gql\]?";
            var subFragments = Regex.Matches(fragment, subFragmentRegex);
            for (var i = 0; i < subFragments.Count; i++)
                fragment = Regex.Replace(fragment, subFragmentRegex, " {\r    ...$1\r  }");

            return Regex.Replace(fragment, @"^(\s+[\w\d]+)\:.*$", "$1", RegexOptions.Multiline);
        }

        private static string MutationOperationName(string entityName) => $"Save{entityName}";

        private static string GetMutation(string entityName)
        {
            var camelCaseEntityName = CamelCaseEntityName(entityName);
            var mutationOperationName = MutationOperationName(entityName);
            return $@"
mutation {mutationOperationName}(${camelCaseEntityName}: Input{entityName}Gql!) {{
  {CamelCaseEntityName(mutationOperationName)}Result({camelCaseEntityName}: ${camelCaseEntityName}) {{
    ...{entityName}
  }}
}}
".Trim();
        }

        private static string QueryOperationName(string entityName) => CamelCaseEntityName(entityName);

        private static string GetQuery(string entityName)
        {
            return $@"
query {entityName}($id: Int!) {{
  {QueryOperationName(entityName)}(id: $id) {{
    ...{entityName}
  }}
}}
".Trim();
        }

        #region Color Console

        private const ConsoleColor GoodColor = ConsoleColor.Green;
        private const ConsoleColor InfoColor = ConsoleColor.Yellow;

        private static void ConsoleWrite(object text, ConsoleColor? color = null, bool endOfLine = false)
        {
            if (color != null)
                Console.ForegroundColor = color.Value;
            Console.Write(text);
            Console.ResetColor();
            if (endOfLine) Console.WriteLine();
        }

        #endregion
    }
}