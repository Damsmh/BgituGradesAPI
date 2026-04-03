using BgituGradesLoader.Compass;
using BgituGradesLoader.Compass.Objects;

namespace BgituGradesLoader.Menu.Panels
{
    public class CompassTestPanel : ConsolePanel
    {
        public override string Title => "Протестировать обращение к API";

        public async override Task Run()
        {
            Console.WriteLine("Получаем список групп...");
            List<CompassGroup>? groups = await CompassManager.GetGroups();

            if (groups is null)
            {
                Console.WriteLine("Список групп пуст");
            }
            else
            {
                foreach (CompassGroup group in groups)
                    Console.WriteLine(group);
            }

            Console.WriteLine();

            Console.WriteLine("Получаем список занятий...");
            List<CompassPair>? pairs = await CompassManager.GetGroupPairs(246);

            if (pairs is null)
            {
                Console.WriteLine("Список занятий пуст");
            }
            else
            {
                foreach (CompassPair pair in pairs)
                    Console.WriteLine(pair);
            }
        }
    }
}
