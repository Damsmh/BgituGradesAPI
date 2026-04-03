using BgituGradesLoader.Menu.Panels;
using BgituGradesLoader.Menu.Panels.DataChange;
using BgituGradesLoader.Save;
using BgituGradesLoader.Table;

namespace BgituGradesLoader.Menu
{
    public class MenuManager
    {
        private readonly List<ConsolePanel> _panels;
        private ConsolePanel? _nowPanel;

        public MenuManager()
        {
            SaveManager saveManager = new();
            TableManager tableManager = new(saveManager);

            _panels = [
                new DataLoaderPanel(saveManager, tableManager),
                new LinkChangePanel(saveManager),
                new TableScrapperTestPanel(tableManager),
                new CompassTestPanel()
            ];
        }

        public async Task Run()
        {
            while (true)
            {
                if (_nowPanel == null)
                {
                    PrintPanelsMenu();
                    if (_nowPanel == null)
                        return;
                }
                else
                {
                    await _nowPanel.Run();
                    Console.WriteLine("Нажмите Enter для возврата");
                    Console.ReadLine();
                    _nowPanel = null;
                }

                Console.Clear();
            }
        }

        private void PrintPanelsMenu()
        {
            for (int i = 0; i < _panels.Count; i++)
                Console.WriteLine($"{i + 1} - {_panels[i].Title}");
            Console.WriteLine("0 — Выход");
            Console.WriteLine();

            Console.Write("Выберите опцию: ");
            int option = Convert.ToInt32(Console.ReadLine());

            if (option == 0)
                return;
            _nowPanel = _panels[option - 1];
        }
    }
}
