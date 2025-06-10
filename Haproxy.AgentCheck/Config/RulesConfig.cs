namespace Lucca.Infra.Haproxy.AgentCheck.Config;

internal class RulesConfig : List<RuleConfig>
{

    public bool AreValid() => TrueForAll(r => r.IsValid());
}
