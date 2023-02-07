using CK.Core;

namespace CK.Readus;

internal class LinkProcessor
{
    public LinkProcessor() { }

    public bool Process
    (
        IActivityMonitor monitor,
        MdDocument[] mdDocuments,
        Func<MdDocument, Action<IActivityMonitor, NormalizedPath>[]> getChecks,
        Func<MdDocument, Func<IActivityMonitor, NormalizedPath, NormalizedPath>[]> getTransforms
    )
    {
        var isOk = true;

        foreach( var mdDocument in mdDocuments )
        {
            var checks = getChecks( mdDocument );
            var transforms = getTransforms( mdDocument );


            using var documentInfo = monitor
                                     .OpenInfo( $"Processing '{mdDocument.OriginPath}'" )
                                     .ConcludeWith( () => $"Processed '{mdDocument.DocumentName}'" );


            foreach( var check in checks )
            {
                using var checkInfo = monitor
                                      .OpenInfo( $"Checking '{mdDocument.DocumentName}' with '{check.Method.Name}'" )
                                      .ConcludeWith( () => $"Checked '{mdDocument.DocumentName}'" );

                mdDocument.CheckLinks( monitor, check );
            }

            foreach( var transform in transforms )
            {
                string DisplaysError() => mdDocument.IsError ? " with error" : string.Empty;
                using var transformInfo = monitor
                                          .OpenInfo( $"Transforming '{mdDocument.DocumentName}' with '{transform.Method.Name}'" )
                                          .ConcludeWith( () => $"Transformed '{mdDocument.DocumentName}'{DisplaysError()}" );

                mdDocument.TransformLinks( monitor, transform );
            }

            isOk = isOk && mdDocument.IsOk;
        }


        if( isOk )
        {
            foreach( var mdDocument in mdDocuments )
            {
                mdDocument.Apply( monitor );
            }
        }

        // Errors

        foreach( var mdDocument in mdDocuments )
        {
            foreach( var link in mdDocument.MarkdownBoundLinks )
            {
                var errors = link.Errors;
            }
        }

        return isOk;
        // return type ? Logs are enough or properties in the object(link)
    }
}

//TODO: I could chain transformations and then when applying (to the markdown ast), solving the file issues.
