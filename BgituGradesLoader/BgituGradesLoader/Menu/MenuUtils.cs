namespace BgituGradesLoader.Menu
{
    public static class MenuUtils
    {
        public static bool GetConfirmFromUser(string confirmMessage)
        {
            Console.WriteLine($"{confirmMessage}? Введите Y/n");
            string? confirm = Console.ReadLine();

            if (confirm == "Y")
                return true;
            return false;
        }
    }
}
