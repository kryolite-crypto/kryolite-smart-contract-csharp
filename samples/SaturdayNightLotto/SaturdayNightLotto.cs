using System.Text;
using Kryolite.SmartContract;

namespace SaturdayNightLotto;

[SmartContract(Name = "Saturday Night Lotto", Url = "https://kryolite-crypto.github.io/kryolottery", ApiLevel = ApiLevel.V1)]
public class SaturdayNightLotto : IKryoliteStandardToken
{
    public static State State { get; }

    static SaturdayNightLotto()
    {
        State = new State
        {
            TicketPrice = 1_000_000_000, // 1000 kryo
            RegistrationOpen = true
        };

        // We need to create instance for API registration
        var instance = new SaturdayNightLotto();

        KryoliteStandardToken.Register(instance);
    }

    [Method]
    [Description("Buy Ticket")]
    public static void BuyTicket()
    {
        Assert.True(State.RegistrationOpen);
        Assert.True(State.TicketPrice == Transaction.Value);

        var fee = Transaction.Value / 100;
        Contract.Owner.Transfer(fee);

        var ticket = PrintTicket();

        State.Tickets.Add(ticket.TokenId, ticket);
        State.TicketToAddress.Add(ticket.TokenId, Transaction.From);

        if (!State.AddressToTickets.TryGetValue(Transaction.From, out var tickets))
        {
            tickets = [];
            State.AddressToTickets.Add(Transaction.From, tickets);
        }

        tickets.Add(ticket);

        var digits = string.Join(string.Empty, ticket.Digits);

        if (!State.DigitsToAddresses.TryGetValue(digits, out var addresses))
        {
            addresses = [];
            State.DigitsToAddresses.Add(digits, addresses);
        }

        addresses.Add(Transaction.From);

        KRC721Event.Transfer(Contract.Address, Transaction.From, ticket.TokenId);
    }

    [Method]
    [Description("Draw Winner")]
    public static void DrawWinner()
    {
        Assert.True(Transaction.From == Contract.Owner);
        Assert.True(State.Tickets.Count > 0);

        State.PreviousWinners.Clear();

        var digits = string.Join(string.Empty, RollDigits());

        State.PreviousDigits = digits;
        
        if(!State.DigitsToAddresses.TryGetValue(digits, out var winners))
        {
            Event.Broadcast(CustomEvents.NoWinner, Contract.Balance);
        }
        else
        {
            // Pay to winners
            var price = Contract.Balance / (ulong)winners.Count;

            foreach (var winner in winners)
            {
                winner.Transfer(price);

                State.PreviousWinners.Add(new Winner
                {
                    Address = winner,
                    Reward = price
                });

                Event.Broadcast(CustomEvents.AnnounceWinner, winner, price);
            }
        }

        // Consume tokens
        foreach (var entry in State.TicketToAddress)
        {
            KRC721Event.Consume(entry.Value, entry.Key);
        }

        // Clear state for next round
        State.Tickets.Clear();
        State.TicketToAddress.Clear();
        State.AddressToTickets.Clear();
        State.DigitsToAddresses.Clear();
    }

    [Method]
    [Description("Open Registration")]
    public static void OpenRegistration()
    {
        Assert.True(Transaction.From == Contract.Owner);
        State.RegistrationOpen = true;
    }

    [Method]
    [Description("Close Registration")]
    public static void CloseRegistration()
    {
        Assert.True(Transaction.From == Contract.Owner);
        State.RegistrationOpen = false;
    }

    [Method]
    [Description("Set ticket price")]
    public static void SetTicketPrice([Description("New price")] ulong newPrice)
    {
        Assert.True(Transaction.From == Contract.Owner);
        Assert.True(State.Tickets.Count == 0);
        Assert.False(State.RegistrationOpen);

        State.TicketPrice = newPrice;
    }

    [Method]
    [Description("Tickets sold")]
    public static string TicketsSold()
    {
        Program.Return(State.Tickets.Count.ToString());
        return State.Tickets.Count.ToString();
    }

    [Method]
    [Description("Get state")]
    public static string GetState()
    {
        return State.ToJson();
    }

    private static Ticket PrintTicket()
    {
        var digits = RollDigits();
        var name = $"Saturday Night Lotto Ticket #{++State.TicketsSold}";
        var hash = Program.HashData(Encoding.UTF8.GetBytes(name));

        return new Ticket
        {
            TokenId = hash,
            Name = name,
            Description = $"Lucky numbers [{digits[0]}, {digits[1]}, {digits[2]}]",
            Digits = digits
        };
    }

    private static byte[] RollDigits()
    {
        var digits = new byte[3];

        for (var i = 0; i < 3; i++)
        {
            digits[i] = (byte)(8 * Program.Rand() + 1);
        }
        
        return digits;
    }

    public StandardToken GetToken(U256 tokenId)
    {
        if (!State.Tickets.TryGetValue(tokenId, out var token))
        {
            Program.Exit(5);
            throw new Exception();
        }

        return new StandardToken
        {
            Name = token.Name,
            Description = token.Description
        };
    }
}

public class State
{
    public int TicketsSold { get; set; }
    public ulong TicketPrice { get; set; }
    public bool RegistrationOpen { get; set; }
    public string PreviousDigits { get; set; } = string.Empty;
    public Dictionary<U256, Ticket> Tickets { get; } = [];
    public Dictionary<U256, Address> TicketToAddress { get; } = [];
    public Dictionary<Address, List<Ticket>> AddressToTickets { get; } = [];
    public Dictionary<string, List<Address>> DigitsToAddresses { get; } = [];
    public Dictionary<U256, Address> ApprovedTransfers { get; } = [];
    public List<Winner> PreviousWinners { get; } = [];

    public string ToJson()
    {
        // It could be possible to use inbuilt JsonConverter
        // but that would add extra 3MB to application size
        // It's not too hard to implement ourself
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendFormat("\"ticketsSold\": {0}", TicketsSold.ToString());
        sb.AppendLine(",");
        sb.AppendFormat("\"ticketPrice\": {0}", TicketPrice.ToString());
        sb.AppendLine(",");
        sb.AppendFormat("\"registrationOpen\": {0}", RegistrationOpen ? "true" : "false");
        sb.AppendLine(",");
        sb.AppendLine("\"tickets\": [");

        var lastAddress = AddressToTickets.LastOrDefault();

        foreach (var entry in AddressToTickets)
        {
            sb.AppendLine("{");
            sb.AppendFormat("\"address\": \"{0}\"", entry.Key.ToString());
            sb.AppendLine(",");
            sb.AppendLine("\"tickets\": [");

            var lastTicket = entry.Value.LastOrDefault();

            foreach (var ticket in entry.Value)
            {
                sb.Append(ticket.ToJson());

                if (ticket != lastTicket)
                {
                    sb.AppendLine(",");
                }
            }

            sb.AppendLine("]}");

            if (entry.Key != lastAddress.Key)
            {
                sb.AppendLine(",");
            }
        }

        sb.AppendLine("]}");

        return sb.ToString();
    }
}

public class Winner
{
    public Address Address { get; set; } = Address.NULL_ADDRESS;
    public ulong Reward { get; set; }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendFormat("\"address\": \"{0}\"", Address.ToString());
        sb.AppendLine(",");
        sb.AppendFormat("\"reward\": {0}", Reward.ToString());
        sb.AppendLine("}");
        return sb.ToString();
    }
}

public class Ticket
{
    public U256 TokenId { get; init; } = U256.NULL_U256;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public byte[] Digits { get; init; } = Array.Empty<byte>();

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendFormat("\"tokenId\": \"{0}\"", TokenId.ToString());
        sb.AppendLine(",");
        sb.AppendFormat("\"name\": \"{0}\"", Name);
        sb.AppendLine(",");
        sb.AppendFormat("\"description\": \"{0}\"\n", Description);
        sb.AppendLine("}");
        return sb.ToString();
    }
}

public enum CustomEvents
{
    AnnounceWinner,
    NoWinner
}