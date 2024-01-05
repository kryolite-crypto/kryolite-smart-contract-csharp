namespace Kryolite.SmartContract;

public static class Assert
{
    public static void True(bool condition)
    {
        if (!condition)
        {
            Program.Exit(127);
        }
    }

    public static void False(bool condition)
    {
        if (condition)
        {
            Program.Exit(127);
        }
    }
}
