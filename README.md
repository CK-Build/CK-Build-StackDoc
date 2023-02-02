# Work In Progress

## TODO

Implement :

- Handle virtual root ~
- Generate ToC
- Transformers idempotent => Tests several runs of the same one, then all in different orders
- Support github branches link
- Copy internal targeted files (img and cs code)
- Check url availability
- A link to a directory should look for a README.md (or index)
- Handle file extension transformation in the right component
- Resolve dots at some point

Test :

- Test stack with a context ready to use
- Test helpers
- TestBase helper : create methods that output generated context information like local path to help creating tests.

Other :

- Rename solution and projects
- Cleanup and optimize
- Internal as much as possible
- Getting started (with demo app)

## Notes

```csharp
if( BaseUrl != null
// According to https://github.com/dotnet/runtime/issues/22718
// this is the proper cross-platform way to check whether a uri is absolute or not:
&& Uri.TryCreate( content, UriKind.RelativeOrAbsolute, out var contentUri )
&& !contentUri.IsAbsoluteUri )
{
content = new Uri( BaseUrl, contentUri ).AbsoluteUri;
}
```

https://github.com/xoofx/markdig/blob/master/src/Markdig/Renderers/MarkdownObjectRenderer.cs
