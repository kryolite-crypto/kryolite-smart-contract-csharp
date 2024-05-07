using System.Text;
using Kryolite.SmartContract;

namespace SaturdayNightLotto;

[SmartContract(Name = "Saturday Night Lotto", Url = "https://kryolite-crypto.github.io/kryolottery", ApiLevel = ApiLevel.V1)]
public class SaturdayNightLotto : IKryoliteStandardToken
{
    public State State { get; private set; } = new();

    [Install]
    public void InstallLotto()
    {
        State = new State
        {
            TicketPrice = 100_000_000, // 100 kryo
            RegistrationOpen = true
        };

        ScheduleNextDraw();
    }

    [Uninstall]
    public void UninstallLotto()
    {

    }

    [Method]
    [Description("Buy Ticket")]
    public void BuyTicket()
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

        KRC721Event.Transfer(Contract.Address, Transaction.From, ticket.TokenId, ticket.Name, ticket.Description);
    }

    [Method]
    [Description("Draw Winner")]
    public void DrawWinner()
    {
        Assert.True(Transaction.From == Contract.Owner);

        if (State.Tickets.Count == 0)
        {
            // Nothing to do, schedule next draw and exit
            ScheduleNextDraw();
            return;
        }

        var digits = string.Join(string.Empty, RollDigits());

        State.PreviousWinners.Clear();
        State.PreviousDigits = digits;
        
        if(!State.DigitsToAddresses.TryGetValue(digits, out var winners))
        {
            Event.Broadcast(CustomEvents.NoWinner, Contract.Balance);
        }
        else
        {
            // Pay to winners, 5 kryo is left for gas fees for scheduled draws
            var price = (Contract.Balance - 5_000000) / (long)winners.Count;

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

        // Schedule next draw
        ScheduleNextDraw();
    }

    [Method]
    [Description("Open Registration")]
    public void OpenRegistration()
    {
        Assert.True(Transaction.From == Contract.Owner);
        State.RegistrationOpen = true;
    }

    [Method]
    [Description("Close Registration")]
    public void CloseRegistration()
    {
        Assert.True(Transaction.From == Contract.Owner);
        State.RegistrationOpen = false;
    }

    [Method]
    [Description("Set ticket price")]
    public void SetTicketPrice([Description("New price")] long newPrice)
    {
        Assert.True(Transaction.From == Contract.Owner);
        Assert.True(State.Tickets.Count == 0);
        Assert.False(State.RegistrationOpen);

        State.TicketPrice = newPrice;
    }

    [Method(ReadOnly = true)]
    [Description("Tickets sold")]
    public string TicketsSold()
    {
        return State.Tickets.Count.ToString();
    }

    [Method(ReadOnly = true)]
    [Description("Get state")]
    public string GetState()
    {
        return State.ToJson();
    }

    private void ScheduleNextDraw()
    {
        // Schedule next draw
        Contract.Scheduler(DrawWinner)
            .DayOfWeek(DayOfWeek.Saturday)
            .At(20, 00)
            .Save();
    }

    private Ticket PrintTicket()
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

    private byte[] RollDigits()
    {
        var digits = new byte[3];

        for (var i = 0; i < 3; i++)
        {
            digits[i] = (byte)(8 * Program.Rand() + 1);
        }
        
        return digits;
    }

    [Method]
    [Description("Get token with tokenId")]
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
    public long TicketPrice { get; set; }
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
        sb.AppendFormat("\"currentPot\": {0}", (Contract.Balance - 5_000000).ToString());
        sb.AppendLine(",");
        sb.AppendFormat("\"registrationOpen\": {0}", RegistrationOpen ? "true" : "false");
        sb.AppendLine(",");
        sb.AppendFormat("\"previousDigits\": \"{0}\"", PreviousDigits);
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

        sb.AppendLine("],");
        sb.AppendLine("\"winners\": [");

        var lastWinner = PreviousWinners.LastOrDefault();

        foreach (var entry in PreviousWinners)
        {
          sb.Append(entry.ToJson());

          if (entry != lastWinner)
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
    public long Reward { get; set; }

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
