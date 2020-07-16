# dnk GraphQL Fragments Generator

`dnkGraphQLFragmentsGenerator.exe --help` output:

```
dnkGraphQLFragmentsGenerator:
  Extracts GraphQL Fragments from `--schema-file` using `--fragment-regex` regular expression and saves those into `--output-folder`

Usage:
  dnkGraphQLFragmentsGenerator [options]

Options:
  --schema-file <schema-file>                    Path to GraphQL schema file [default: schema.graphql]
  --output-folder <output-folder>                Path to base output folder [default: src/generated]
  --operation-names <operation-names>            List of queries and mutations to generate [default: ]
  --verbose                                      Turns on verbose logging [default: False]
  --fragment-regex <fragment-regex>              Regular expression to find fragments in schema file [default: ^type \w+Gql \{.+?\}$]
  --fragment-name-regex <fragment-name-regex>    Regular expression to extract fragment name from --fragment-regex [default: ^type (?:(\w+)Gql).*$]
  --version                                      Show version information
  -?, -h, --help                                 Show help and usage information
```