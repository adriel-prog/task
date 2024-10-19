using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Text.Json;
using System.Linq;
using System;

namespace task
{
    public partial class MainPage : ContentPage
    {
        public HabitViewModel ViewModel { get; set; }

        public MainPage()
        {
            InitializeComponent();
            ViewModel = new HabitViewModel();
            BindingContext = ViewModel;
        }
    }

    public class Habit
    {
        public string Title { get; set; }
        public int Frequency { get; set; }
        public int Count { get; set; }
        public bool IsCompleted => Count >= Frequency; // Propriedade para verificar se o hábito foi concluído

        public int RemainingCount => Frequency - Count; // Propriedade para mostrar quanto falta para concluir
    }

    public class HabitStorage
    {
        private const string HabitKey = "habits";

        public static List<Habit> LoadHabits()
        {
            var habitsJson = Preferences.Get(HabitKey, "[]");
            return JsonSerializer.Deserialize<List<Habit>>(habitsJson) ?? new List<Habit>();
        }

        public static void SaveHabits(List<Habit> habits)
        {
            var habitsJson = JsonSerializer.Serialize(habits);
            Preferences.Set(HabitKey, habitsJson);
        }
    }

    public class HabitViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Habit> ActiveHabits { get; set; }
        public ObservableCollection<Habit> CompletedHabits { get; set; }
        public string NewHabitTitle { get; set; }
        public int NewHabitFrequency { get; set; }

        public ICommand AddHabitCommand => new Command(AddHabit);
        public ICommand CompleteHabitCommand => new Command<Habit>(CompleteHabit);
        public ICommand RemoveHabitCommand => new Command<Habit>(RemoveHabit);

        private DateTime lastResetDate;

        public HabitViewModel()
        {
            ActiveHabits = new ObservableCollection<Habit>(HabitStorage.LoadHabits());
            CompletedHabits = new ObservableCollection<Habit>();
            lastResetDate = DateTime.Today; // Inicializa a data do último reset
            ResetDailyProgress();
        }

        private void AddHabit()
        {
            if (string.IsNullOrWhiteSpace(NewHabitTitle) || NewHabitFrequency <= 0)
            {
                // Lógica de tratamento de erro ou notificação ao usuário
                return;
            }

            var newHabit = new Habit { Title = NewHabitTitle, Frequency = NewHabitFrequency, Count = 0 };
            ActiveHabits.Add(newHabit);
            HabitStorage.SaveHabits(ActiveHabits.ToList());
            NewHabitTitle = string.Empty; // Limpa o campo de entrada
            NewHabitFrequency = 0; // Reseta a frequência
            OnPropertyChanged(nameof(NewHabitTitle));
            OnPropertyChanged(nameof(NewHabitFrequency));
        }

        private void CompleteHabit(Habit habit)
        {
            if (habit.Count < habit.Frequency) // Verifica se não excede a frequência
            {
                habit.Count++;
                HabitStorage.SaveHabits(ActiveHabits.ToList());

                // Atualiza a interface
                OnPropertyChanged(nameof(ActiveHabits));

                // Se o hábito estiver concluído, mova-o para a lista de hábitos concluídos
                if (habit.IsCompleted)
                {
                    ActiveHabits.Remove(habit);
                    CompletedHabits.Add(habit);
                    OnPropertyChanged(nameof(CompletedHabits)); // Atualiza a lista de hábitos concluídos
                }
            }
        }

        private void RemoveHabit(Habit habit)
        {
            if (CompletedHabits.Contains(habit))
            {
                CompletedHabits.Remove(habit);
            }
            else
            {
                ActiveHabits.Remove(habit);
            }
            HabitStorage.SaveHabits(ActiveHabits.ToList());
        }

        private void ResetDailyProgress()
        {
            if (DateTime.Today > lastResetDate)
            {
                foreach (var habit in ActiveHabits)
                {
                    habit.Count = 0; // Reseta o contador diário
                }
                lastResetDate = DateTime.Today; // Atualiza a data do último reset
                HabitStorage.SaveHabits(ActiveHabits.ToList());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
