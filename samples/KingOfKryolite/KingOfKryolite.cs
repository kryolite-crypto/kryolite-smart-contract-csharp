﻿using System.Runtime.InteropServices;
using System.Text;
using Kryolite.SmartContract;

namespace KingOfKryolite;

[SmartContract(Name = "King of Kryolite", Url = "https://kryolite-crypto.github.io/kingofkryolite", ApiLevel = ApiLevel.V1)]
public class KingOfKryolite
{
    private readonly State State = new();

    [Install]
    public void InstallContract()
    {
        State.ClaimAmount = 100_000000;
    }

    [Uninstall]
    public void UninstallContract()
    {

    }

    [Method]
    [Description("Claim Throne")]
    public void ClaimThrone([Description("Name for your king")] string name)
    {
        Assert.True(Transaction.Value >= State.ClaimAmount);

        if (State.Kings.Count > 0)
        {
            var king = State.CurrentKing;

            // Claim throne by buying it from current king
            king.Address.Transfer(Transaction.Value);
            king.Profit = Transaction.Value - king.ClaimAmount;

            Event.Broadcast(CustomEvents.OldKing, king.Name);
        }

        var newKing = new King
        {
            Name = name,
            Address = Transaction.From,
            ClaimAmount = Transaction.Value,
            Timestamp = View.Timestamp
        };

        State.Kings.Add(newKing);

        // Increase ClaimAmount
        State.ClaimAmount = (long)(Transaction.Value * 1.25d);

        Event.Broadcast(CustomEvents.NewKing, name, Transaction.Value, State.ClaimAmount);
    }

    [Method(ReadOnly = true)]
    [Description("Get State")]
    public string GetState()
    {
        return State.ToJson();
    }
}

public class State
{
    public long ClaimAmount;
    public List<King> Kings = new();
    public King CurrentKing => Kings[Kings.Count - 1];

    public string ToJson()
    {
        // It could be possible to use inbuilt JsonConverter
        // but that would add extra 3MB to application size
        // It's not too hard to implement ourself
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendFormat("\"claimAmount\": {0}", ClaimAmount.ToString());
        sb.AppendLine(",");
        sb.AppendLine("\"kings\": [");

        var lastKing = Kings.LastOrDefault();

        foreach (var king in Kings)
        {
            sb.Append(king.ToJson());

            if (king != lastKing)
            {
                sb.AppendLine(",");
            }
        }

        sb.AppendLine("]}");

        return sb.ToString();
    }
}

public class King
{
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = Address.NULL_ADDRESS;
    public long ClaimAmount { get; set; }
    public long Profit { get; set; }
    public long Timestamp { get; set; }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendFormat("\"name\": \"{0}\"", Name);
        sb.AppendLine(",");
        sb.AppendFormat("\"address\": \"{0}\"", Address.ToString());
        sb.AppendLine(",");
        sb.AppendFormat("\"claimAmount\": {0}", ClaimAmount.ToString());
        sb.AppendLine(",");
        sb.AppendFormat("\"profit\": {0}", Profit.ToString());
        sb.AppendLine(",");
        sb.AppendFormat("\"timestamp\": {0}", Timestamp.ToString());
        sb.AppendLine("}");
        return sb.ToString();
    }
}

public enum CustomEvents
{
    NewKing,
    OldKing
}