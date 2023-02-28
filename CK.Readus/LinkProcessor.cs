using CK.Core;

namespace CK.Readus;

internal class LinkProcessor
{
    public LinkProcessor() { }

    public async Task<bool> ProcessAsync
    (
        IActivityMonitor monitor,
        MdDocument[] mdDocuments,
        Func<MdDocument, Action<IActivityMonitor, NormalizedPath>[]>? getChecks,
        Func<MdDocument, Func<IActivityMonitor, NormalizedPath, Task>[]>? getAsyncChecks,
        Func<MdDocument, Func<IActivityMonitor, NormalizedPath, NormalizedPath>[]> getTransforms
    )
    {
        var isOk = true;

        // await Task.WhenAll
        // (
        //     mdDocuments.Select<MdDocument, Task>
        //     (
        //         async mdDocument => { await ProcessDocumentAsync( monitor, mdDocument ); }
        //     )
        // );

        foreach ( var mdDocument in mdDocuments )
        {
            await ProcessDocumentAsync( monitor, mdDocument );
        }

        async Task ProcessDocumentAsync( IActivityMonitor m, MdDocument mdDocument )
        {
            var checks = getChecks?.Invoke( mdDocument );
            var asyncChecks = getAsyncChecks?.Invoke( mdDocument );
            var transforms = getTransforms( mdDocument );


            using var documentInfo = m
                                     .OpenInfo( $"Processing '{mdDocument.LocalPath}'" )
                                     .ConcludeWith( () => $"Processed '{mdDocument.DocumentName}'" );


            if ( checks != null )
                foreach ( var check in checks )
                {
                    using var checkInfo = m
                                          .OpenInfo
                                          ( $"Checking '{mdDocument.DocumentName}' with '{check.Method.Name}'" )
                                          .ConcludeWith( () => $"Checked '{mdDocument.DocumentName}'" );

                    mdDocument.CheckLinks( m, check );
                }

            if ( asyncChecks != null )
                foreach ( var check in asyncChecks )
                {
                    using var checkInfo = m
                                          .OpenInfo
                                          (
                                              $"Checking '{mdDocument.DocumentName}' with '{check.Method.Name}'"
                                          )
                                          .ConcludeWith( () => $"Checked '{mdDocument.DocumentName}'" );

                    await mdDocument.CheckLinksAsync( m, check );
                }


            foreach ( var transform in transforms )
            {
                string DisplaysError() => mdDocument.IsError ? " with error" : string.Empty;
                using var transformInfo = m
                                          .OpenInfo
                                          (
                                              $"Transforming '{mdDocument.DocumentName}' with '{transform.Method.Name}'"
                                          )
                                          .ConcludeWith
                                          ( () => $"Transformed '{mdDocument.DocumentName}'{DisplaysError()}" );

                mdDocument.TransformLinks( m, transform );
            }

            isOk = isOk && mdDocument.IsOk;
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
