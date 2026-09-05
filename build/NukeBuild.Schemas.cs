using Drift.Build.Utilities;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Target = Nuke.Common.Target;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  Target GenerateSchemas => _ => _
    .DependsOn( GenerateSpecSchema, GenerateSettingsSchema );

  Target GenerateSpecSchema => _ => _
    .Executes( () => {
        using var _ = new OperationTimer( nameof(GenerateSpecSchema) );

        RunSchemaGenerator(
          Solution.Spec_SchemaGenerator_Cli.Path,
          Solution.Spec.Directory / "embedded_resources" / "schemas"
        );
      }
    );

  Target GenerateSettingsSchema => _ => _
    .Executes( () => {
        using var _ = new OperationTimer( nameof(GenerateSettingsSchema) );

        RunSchemaGenerator(
          Solution.Cli.Cli_Settings_SchemaGenerator_Cli.Path,
          Solution.Cli.Cli_Settings.Directory / "embedded_resources" / "schemas"
        );
      }
    );

  private void RunSchemaGenerator( AbsolutePath projectFile, AbsolutePath outputDirectory ) {
    Log.Information( "Generating schema from {Project} into {OutputDirectory}", projectFile, outputDirectory );

    DotNetRun( s => s
      .SetProjectFile( projectFile )
      .SetConfiguration( Configuration )
      .SetApplicationArguments( outputDirectory )
    );
  }
}