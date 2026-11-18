using System.Linq;
using Jellyfin.Plugin.PremiumUI.Library;
using Xunit;

namespace Jellyfin.Plugin.PremiumUI.Tests;

public class StudioGrouperTests
{
    [Fact]
    public void Group_DisneyDerivatives_ProduceOneBrandGroup()
    {
        var studios = Entries(
            "Disney Channel",
            "Disney Television Animation",
            "DisneyToon Studios",
            "Walt Disney Animation",
            "Walt Disney Pictures",
            "Walt Disney Productions");

        var group = Assert.Single(StudioGrouper.Group(studios));

        Assert.Equal("brand:disney", group.Key);
        Assert.Equal("Disney", group.Name);
        Assert.Equal(6, group.Members.Count);
    }

    [Fact]
    public void Group_CorporateSuffixes_AreIgnored()
    {
        var studios = Entries(
            "Working Title",
            "Working Title Films",
            "Working Title Television");

        var group = Assert.Single(StudioGrouper.Group(studios));

        Assert.Equal("Working Title", group.Name);
        Assert.Equal(3, group.Members.Count);
    }

    [Fact]
    public void Group_FoxAnd20thCentury_ShareOneBrand()
    {
        var studios = Entries(
            "Fox Searchlight Pictures",
            "20th Century Fox",
            "20th Century Studios",
            "20th Television");

        var group = Assert.Single(StudioGrouper.Group(studios));

        Assert.Equal("brand:fox", group.Key);
        Assert.Equal("20th Century Fox", group.Name);
        Assert.Equal(4, group.Members.Count);
    }

    [Fact]
    public void Group_SonyPrefix_DoesNotSwallowNipponColumbia()
    {
        var studios = Entries(
            "Sony Pictures",
            "Columbia Pictures",
            "TriStar Pictures",
            "Nippon Columbia");

        var group = Assert.Single(StudioGrouper.Group(studios));

        Assert.Equal("Sony Pictures", group.Name);
        Assert.DoesNotContain(group.Members, member => member.Name == "Nippon Columbia");
    }

    [Fact]
    public void Group_CartoonNetwork_DoesNotSwallowOtherCartoons()
    {
        var studios = Entries(
            "Cartoon Network",
            "Cartoon Network Studios",
            "Atomic Cartoons",
            "a.k.a. Cartoon");

        var group = Assert.Single(StudioGrouper.Group(studios));

        Assert.Equal("Cartoon Network", group.Name);
        Assert.DoesNotContain(group.Members, member => member.Name is "Atomic Cartoons" or "a.k.a. Cartoon");
    }

    [Fact]
    public void Group_SharedFirstWord_IsNotEnough()
    {
        var studios = Entries(
            "Silver Pictures",
            "Silver Reel",
            "Screen Australia",
            "Screen Gems",
            "Media United",
            "United Films");

        Assert.Empty(StudioGrouper.Group(studios));
    }

    [Fact]
    public void Group_AppleBrand_DoesNotSwallowApplecross()
    {
        var studios = Entries("Apple Studios", "Apple TV", "Applecross");

        var group = Assert.Single(StudioGrouper.Group(studios));

        Assert.Equal("Apple", group.Name);
        Assert.DoesNotContain(group.Members, member => member.Name == "Applecross");
    }

    [Fact]
    public void Resolve_ConfiguredAlias_ExpandsToWholeGroup()
    {
        var studios = Entries("Disney Channel", "Walt Disney Pictures", "Walt Disney Animation");
        var groups = StudioGrouper.Group(studios);

        var collection = Assert.Single(StudioGrouper.Resolve(groups, studios, ["Walt Disney Pictures"]));

        Assert.Equal("Disney", collection.Name);
        Assert.Equal(3, collection.Members.Count);
    }

    [Fact]
    public void Resolve_DuplicateAliases_ReturnOneCollection()
    {
        var studios = Entries("Disney Channel", "Walt Disney Pictures", "A24");
        var groups = StudioGrouper.Group(studios);

        var collections = StudioGrouper.Resolve(groups, studios, ["Disney", "Disney Channel", "A24"]);

        Assert.Equal(2, collections.Count);
        Assert.Equal("Disney", collections[0].Name);
        Assert.Equal("A24", collections[1].Name);
    }

    [Fact]
    public void Resolve_BrandLabelWithoutExactStudio_StillMatches()
    {
        var studios = Entries("Marvel Comics", "Marvel Studios");
        var groups = StudioGrouper.Group(studios);

        var collection = Assert.Single(StudioGrouper.Resolve(groups, studios, ["Marvel"]));

        Assert.Equal("Marvel", collection.Name);
        Assert.Equal(2, collection.Members.Count);
    }

    [Fact]
    public void Resolve_UnknownName_IsIgnored()
    {
        var studios = Entries("A24");

        var collections = StudioGrouper.Resolve([], studios, ["No existe"]);

        Assert.Empty(collections);
    }

    private static StudioEntry[] Entries(params string[] names)
        => names
            .Select((name, index) => new StudioEntry(index.ToString("x32"), name))
            .ToArray();
}
