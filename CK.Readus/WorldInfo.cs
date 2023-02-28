namespace CK.Readus;

public sealed class WorldInfo : IEquatable<WorldInfo>
{
    public string Name { get; }
    public string Version { get; }

    public WorldInfo( string name, string version )
    {
        Name = name;
        Version = version;
    }

    /// <inheritdoc />
    public bool Equals( WorldInfo? other )
    {
        if( ReferenceEquals( null, other ) ) return false;
        if( ReferenceEquals( this, other ) ) return true;
        return string.Equals( Name, other.Name, StringComparison.InvariantCulture )
            && string.Equals( Version, other.Version, StringComparison.InvariantCulture );
    }

    /// <inheritdoc />
    public override bool Equals( object? obj )
    {
        if( ReferenceEquals( null, obj ) ) return false;
        if( ReferenceEquals( this, obj ) ) return true;
        if( obj.GetType() != this.GetType() ) return false;
        return Equals( (WorldInfo)obj );
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add( Name, StringComparer.InvariantCulture );
        hashCode.Add( Version, StringComparer.InvariantCulture );
        return hashCode.ToHashCode();
    }
}
