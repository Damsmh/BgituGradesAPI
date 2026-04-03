using BgituGradesLoader.Save;
using BgituGradesLoader.Save.Data;

namespace BgituGradesLoader.Menu.Panels.DataChange
{
    public abstract class DataChangePanel<T> : ConsolePanel
    {
        protected readonly SaveManager _saveManager;
        protected readonly SaveDataField<T> _dataField;

        public DataChangePanel(SaveManager saveManager)
        {
            _saveManager = saveManager;
            _dataField = GetDataFieldFromSaveManager();
        }

        public async override Task Run()
        {
            await Task.Run(UpdateData);
        }

        private void UpdateData()
        {
            Console.WriteLine($"Текущее значение: {_dataField.Data}");
            Console.WriteLine($"Последнее изменение: {_dataField.LastChange}");

            bool changeConfirm = MenuUtils.GetConfirmFromUser("Вы действительно хотите изменить значение");
            if (!changeConfirm)
                return;

            T newData = GetNewDataFromUser();
            _dataField.Data = newData;
            _dataField.LastChange = DateTime.Now;
            _saveManager.Save();
        }

        protected abstract T GetNewDataFromUser();
        protected abstract SaveDataField<T> GetDataFieldFromSaveManager();
    }
}
