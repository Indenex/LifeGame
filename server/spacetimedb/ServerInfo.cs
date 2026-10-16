using LifeGame.Shared;
using SpacetimeDB;

// Which build of the module is running, so a client can tell a mismatched server
// apart from a broken one (bugfix-7). Build is the git commit the module was
// built from (BuildStamp, written at build time by shared/BuildStamp.targets).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ServerInfo", Public = true)]
    public partial struct ServerInfo
    {
        [PrimaryKey]
        public uint Id;
        public string Build;
    }

    // Safe to call any time: a module published over an older world without a
    // wipe never runs Init, so connecting stamps it too.
    static void EnsureServerInfo(ReducerContext ctx)
    {
        if (ctx.Db.ServerInfo.Id.Find(0) is { } info)
        {
            if (info.Build != BuildStamp.Commit) ctx.Db.ServerInfo.Id.Update(info with { Build = BuildStamp.Commit });
        }
        else ctx.Db.ServerInfo.Insert(new ServerInfo { Id = 0, Build = BuildStamp.Commit });
    }
}
