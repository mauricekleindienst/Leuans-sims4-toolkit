using ModernDesign.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ModernDesign.MVVM.View
{
    public partial class CheatsGuideView : Window
    {
        private List<CheatItem> _allCheats = new List<CheatItem>();
        private string _selectedCategory = "All";
        private HashSet<string> _favoriteCommands = new HashSet<string>();

        public CheatsGuideView()
        {
            InitializeComponent();
            LoadFavorites();
            ApplyLanguage();
            InitializeCheats();
        }

        private void ApplyLanguage()
        {
            bool es = LanguageManager.IsSpanish;

            this.Title = es ? "Guía de Trucos" : "Cheats Guide";
            TitleText.Text = es ? "🎮 Guía de Trucos" : "🎮 Cheats Guide";
            SubtitleText.Text = es
                ? "Lista completa de códigos de trucos de Los Sims 4"
                : "Complete list of The Sims 4 cheat codes";

            SearchBox.Text = es ? "Buscar trucos..." : "Search cheats...";

            ExportAllButton.Content = es ? "📥 Exportar Todo" : "📥 Export All";
            ExportAllButton.ToolTip = es
                ? "Exportar todos los trucos a un archivo .txt en el Escritorio"
                : "Export all cheats to a .txt file on Desktop";

            ExportFavoritesButton.Content = es ? "⭐ Exportar Favoritos" : "⭐ Export Favorites";
            ExportFavoritesButton.ToolTip = es
                ? "Exportar solo los trucos favoritos a un archivo .txt en el Escritorio"
                : "Export only favorite cheats to a .txt file on Desktop";
        }

        private void LoadFavorites()
        {
            try
            {
                string favoritesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ModernDesign",
                    "CheatsFavorites.txt"
                );

                if (File.Exists(favoritesPath))
                {
                    var lines = File.ReadAllLines(favoritesPath);
                    _favoriteCommands = new HashSet<string>(lines);
                }
            }
            catch
            {
                // Si hay error, simplemente no cargamos favoritos
            }
        }

        private void SaveFavorites()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ModernDesign"
                );

                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                string favoritesPath = Path.Combine(appDataPath, "CheatsFavorites.txt");
                File.WriteAllLines(favoritesPath, _favoriteCommands);
            }
            catch
            {
                // Si hay error al guardar, ignoramos
            }
        }

        private void InitializeCheats()
        {
            bool es = LanguageManager.IsSpanish;

            _allCheats = new List<CheatItem>
            {
                // BASIC CHEATS
                new CheatItem
                {
                    Category = es ? "Básicos" : "Basic",
                    Command = "testingcheats true",
                    Name = es ? "Activar Trucos" : "Enable Cheats",
                    Description = es
                        ? "Activa los trucos en el juego. DEBE activarse primero antes de usar otros trucos."
                        : "Enables cheats in the game. MUST be activated first before using other cheats.",
                    Usage = es ? "Escribe en la consola: testingcheats true" : "Type in console: testingcheats true"
                },
                new CheatItem
                {
                    Category = es ? "Básicos" : "Basic",
                    Command = "testingcheats false",
                    Name = es ? "Desactivar Trucos" : "Disable Cheats",
                    Description = es
                        ? "Desactiva los trucos en el juego."
                        : "Disables cheats in the game.",
                    Usage = es ? "Escribe en la consola: testingcheats false" : "Type in console: testingcheats false"
                },
                new CheatItem
                {
                    Category = es ? "Básicos" : "Basic",
                    Command = "headlineeffects on/off",
                    Name = es ? "Efectos de Título" : "Headline Effects",
                    Description = es
                        ? "Activa o desactiva los efectos sobre la cabeza de los Sims (plumbob y pensamientos)."
                        : "Enables or disables effects above Sims' heads (plumbob and thoughts).",
                    Usage = es ? "headlineeffects on o headlineeffects off" : "headlineeffects on or headlineeffects off"
                },
                new CheatItem
                {
                    Category = es ? "Básicos" : "Basic",
                    Command = "fps on/off",
                    Name = "FPS",
                    Description = es
                        ? "Muestra u oculta el contador de FPS en la esquina superior."
                        : "Shows or hides the FPS counter in the top corner.",
                    Usage = "fps on / fps off"
                },
                new CheatItem
                {
                    Category = es ? "Básicos" : "Basic",
                    Command = "fullscreen",
                    Name = es ? "Pantalla Completa" : "Fullscreen",
                    Description = es
                        ? "Alterna entre modo ventana y pantalla completa."
                        : "Toggles between windowed and fullscreen mode.",
                    Usage = "fullscreen"
                },
                new CheatItem
                {
                    Category = es ? "Básicos" : "Basic",
                    Command = "hovereffects on/off",
                    Name = es ? "Efectos Hover" : "Hover Effects",
                    Description = es
                        ? "Activa o desactiva el resaltado al pasar el mouse sobre objetos."
                        : "Enables or disables highlighting when hovering over objects.",
                    Usage = "hovereffects on / hovereffects off"
                },

                // MONEY CHEATS
                new CheatItem
                {
                    Category = es ? "Dinero" : "Money",
                    Command = "motherlode",
                    Name = "Motherlode",
                    Description = es
                        ? "Añade §50,000 simoleones a tu hogar."
                        : "Adds §50,000 simoleons to your household.",
                    Usage = "motherlode"
                },
                new CheatItem
                {
                    Category = es ? "Dinero" : "Money",
                    Command = "kaching",
                    Name = "Kaching",
                    Description = es
                        ? "Añade §1,000 simoleones a tu hogar."
                        : "Adds §1,000 simoleons to your household.",
                    Usage = "kaching"
                },
                new CheatItem
                {
                    Category = es ? "Dinero" : "Money",
                    Command = "rosebud",
                    Name = "Rosebud",
                    Description = es
                        ? "Añade §1,000 simoleones a tu hogar (igual que kaching)."
                        : "Adds §1,000 simoleons to your household (same as kaching).",
                    Usage = "rosebud"
                },
                new CheatItem
                {
                    Category = es ? "Dinero" : "Money",
                    Command = "money [cantidad]",
                    Name = es ? "Dinero Exacto" : "Exact Money",
                    Description = es
                        ? "Establece la cantidad exacta de dinero que deseas. Requiere testingcheats true."
                        : "Sets the exact amount of money you want. Requires testingcheats true.",
                    Usage = es ? "money 1000000 (reemplaza con la cantidad deseada)" : "money 1000000 (replace with desired amount)"
                },
                new CheatItem
                {
                    Category = es ? "Dinero" : "Money",
                    Command = "household.autopay_bills true/false",
                    Name = es ? "Auto-pagar Facturas" : "Auto-pay Bills",
                    Description = es
                        ? "Activa o desactiva el pago automático de facturas."
                        : "Enables or disables automatic bill payment.",
                    Usage = "household.autopay_bills true / false"
                },
                new CheatItem
                {
                    Category = es ? "Dinero" : "Money",
                    Command = "FreeRealEstate on/off",
                    Name = es ? "Bienes Raíces Gratis" : "Free Real Estate",
                    Description = es
                        ? "Hace que todas las casas y lotes sean gratis al mudarse."
                        : "Makes all houses and lots free when moving in.",
                    Usage = "FreeRealEstate on / off"
                },

                // NEEDS CHEATS
                new CheatItem
                {
                    Category = es ? "Necesidades" : "Needs",
                    Command = "fillmotive motive_[tipo]",
                    Name = es ? "Llenar Necesidad" : "Fill Need",
                    Description = es
                        ? "Llena una necesidad específica del Sim seleccionado. Tipos: hunger, energy, bladder, hygiene, social, fun."
                        : "Fills a specific need of the selected Sim. Types: hunger, energy, bladder, hygiene, social, fun.",
                    Usage = "fillmotive motive_hunger"
                },
                new CheatItem
                {
                    Category = es ? "Necesidades" : "Needs",
                    Command = "sims.fill_all_commodities",
                    Name = es ? "Llenar Todas las Necesidades" : "Fill All Needs",
                    Description = es
                        ? "Llena todas las necesidades del Sim activo."
                        : "Fills all needs of the active Sim.",
                    Usage = "sims.fill_all_commodities"
                },
                new CheatItem
                {
                    Category = es ? "Necesidades" : "Needs",
                    Command = "sims.disable_all_commodities",
                    Name = es ? "Desactivar Decaimiento de Necesidades" : "Disable Needs Decay",
                    Description = es
                        ? "Las necesidades no disminuirán. Requiere testingcheats true."
                        : "Needs will not decay. Requires testingcheats true.",
                    Usage = "sims.disable_all_commodities"
                },
                new CheatItem
                {
                    Category = es ? "Necesidades" : "Needs",
                    Command = "sims.enable_all_commodities",
                    Name = es ? "Activar Decaimiento de Necesidades" : "Enable Needs Decay",
                    Description = es
                        ? "Reactiva el decaimiento normal de necesidades."
                        : "Re-enables normal needs decay.",
                    Usage = "sims.enable_all_commodities"
                },

                // SKILLS CHEATS
                new CheatItem
                {
                    Category = es ? "Habilidades" : "Skills",
                    Command = "stats.set_skill_level Major_[habilidad] [nivel]",
                    Name = es ? "Establecer Habilidad Mayor" : "Set Major Skill",
                    Description = es
                        ? "Establece el nivel de una habilidad mayor (1-10). Habilidades: Baking, Bartending, Charisma, Comedy, Fishing, Fitness, Gardening, GourmetCooking, Guitar, Handiness, HomestyleCooking, Logic, Mischief, Painting, Photography, Piano, Programming, RocketScience, Singing, Violin, VideoGaming, Writing."
                        : "Sets the level of a major skill (1-10). Skills: Baking, Bartending, Charisma, Comedy, Fishing, Fitness, Gardening, GourmetCooking, Guitar, Handiness, HomestyleCooking, Logic, Mischief, Painting, Photography, Piano, Programming, RocketScience, Singing, Violin, VideoGaming, Writing.",
                    Usage = "stats.set_skill_level Major_Painting 10"
                },
                new CheatItem
                {
                    Category = es ? "Habilidades" : "Skills",
                    Command = "stats.set_skill_level Minor_[habilidad] [nivel]",
                    Name = es ? "Establecer Habilidad Menor" : "Set Minor Skill",
                    Description = es
                        ? "Establece el nivel de una habilidad menor (1-5). Habilidades: Dancing, DJMixing, MediaProduction, Wellness."
                        : "Sets the level of a minor skill (1-5). Skills: Dancing, DJMixing, MediaProduction, Wellness.",
                    Usage = "stats.set_skill_level Minor_Dancing 5"
                },
                new CheatItem
                {
                    Category = es ? "Habilidades" : "Skills",
                    Command = "stats.set_skill_level Skill_Child_[habilidad] [nivel]",
                    Name = es ? "Habilidad de Niño" : "Child Skill",
                    Description = es
                        ? "Establece habilidades de niños (1-10). Habilidades: Creativity, Mental, Motor, Social."
                        : "Sets child skills (1-10). Skills: Creativity, Mental, Motor, Social.",
                    Usage = "stats.set_skill_level Skill_Child_Creativity 10"
                },
                new CheatItem
                {
                    Category = es ? "Habilidades" : "Skills",
                    Command = "stats.set_skill_level Skill_Toddler_[habilidad] [nivel]",
                    Name = es ? "Habilidad de Infante" : "Toddler Skill",
                    Description = es
                        ? "Establece habilidades de infantes (1-5). Habilidades: Communication, Imagination, Movement, Potty, Thinking."
                        : "Sets toddler skills (1-5). Skills: Communication, Imagination, Movement, Potty, Thinking.",
                    Usage = "stats.set_skill_level Skill_Toddler_Thinking 5"
                },

                // CAREER CHEATS
                new CheatItem
                {
                    Category = es ? "Carreras" : "Careers",
                    Command = "careers.promote [carrera]",
                    Name = es ? "Promoción Instantánea" : "Instant Promotion",
                    Description = es
                        ? "Promociona al Sim en su carrera actual. Carreras comunes: Adult_Active_Astronaut, Adult_Active_Athlete, Adult_Criminal, Adult_Culinary, Adult_Entertainer, Adult_Painter, Adult_SecretAgent, Adult_TechGuru, Adult_Writer."
                        : "Promotes the Sim in their current career. Common careers: Adult_Active_Astronaut, Adult_Active_Athlete, Adult_Criminal, Adult_Culinary, Adult_Entertainer, Adult_Painter, Adult_SecretAgent, Adult_TechGuru, Adult_Writer.",
                    Usage = "careers.promote Adult_Painter"
                },
                new CheatItem
                {
                    Category = es ? "Carreras" : "Careers",
                    Command = "careers.demote [carrera]",
                    Name = es ? "Degradar Carrera" : "Demote Career",
                    Description = es
                        ? "Degrada al Sim un nivel en su carrera."
                        : "Demotes the Sim one level in their career.",
                    Usage = "careers.demote Adult_Painter"
                },
                new CheatItem
                {
                    Category = es ? "Carreras" : "Careers",
                    Command = "careers.add_career [carrera]",
                    Name = es ? "Añadir Carrera" : "Add Career",
                    Description = es
                        ? "Añade una carrera específica al Sim."
                        : "Adds a specific career to the Sim.",
                    Usage = "careers.add_career Adult_Painter"
                },
                new CheatItem
                {
                    Category = es ? "Carreras" : "Careers",
                    Command = "careers.remove_career [carrera]",
                    Name = es ? "Eliminar Carrera" : "Remove Career",
                    Description = es
                        ? "Elimina la carrera del Sim."
                        : "Removes the career from the Sim.",
                    Usage = "careers.remove_career Adult_Painter"
                },

                // RELATIONSHIPS CHEATS
                new CheatItem
                {
                    Category = es ? "Relaciones" : "Relationships",
                    Command = "modifyrelationship [Sim1] [Sim2] [cantidad] LTR_Friendship_Main",
                    Name = es ? "Modificar Amistad" : "Modify Friendship",
                    Description = es
                        ? "Modifica el nivel de amistad entre dos Sims (-100 a 100)."
                        : "Modifies the friendship level between two Sims (-100 to 100).",
                    Usage = "modifyrelationship John Doe Jane Doe 100 LTR_Friendship_Main"
                },
                new CheatItem
                {
                    Category = es ? "Relaciones" : "Relationships",
                    Command = "modifyrelationship [Sim1] [Sim2] [cantidad] LTR_Romance_Main",
                    Name = es ? "Modificar Romance" : "Modify Romance",
                    Description = es
                        ? "Modifica el nivel de romance entre dos Sims (-100 a 100)."
                        : "Modifies the romance level between two Sims (-100 to 100).",
                    Usage = "modifyrelationship John Doe Jane Doe 100 LTR_Romance_Main"
                },
                new CheatItem
                {
                    Category = es ? "Relaciones" : "Relationships",
                    Command = "relationship.introduce_sim_to_all_others",
                    Name = es ? "Conocer a Todos" : "Meet Everyone",
                    Description = es
                        ? "El Sim activo conocerá a todos los Sims del mundo."
                        : "The active Sim will meet all Sims in the world.",
                    Usage = "relationship.introduce_sim_to_all_others"
                },

                // BUILD/BUY CHEATS
                new CheatItem
                {
                    Category = es ? "Construcción" : "Build",
                    Command = "bb.moveobjects on/off",
                    Name = es ? "Mover Objetos" : "Move Objects",
                    Description = es
                        ? "Permite colocar objetos en cualquier lugar, ignorando restricciones."
                        : "Allows placing objects anywhere, ignoring restrictions.",
                    Usage = "bb.moveobjects on"
                },
                new CheatItem
                {
                    Category = es ? "Construcción" : "Build",
                    Command = "bb.showhiddenobjects",
                    Name = es ? "Objetos Ocultos" : "Hidden Objects",
                    Description = es
                        ? "Muestra objetos ocultos en el modo construcción/compra."
                        : "Shows hidden objects in build/buy mode.",
                    Usage = "bb.showhiddenobjects"
                },
                new CheatItem
                {
                    Category = es ? "Construcción" : "Build",
                    Command = "bb.showliveeditobjects",
                    Name = es ? "Objetos de Edición en Vivo" : "Live Edit Objects",
                    Description = es
                        ? "Muestra objetos especiales de edición en vivo."
                        : "Shows special live edit objects.",
                    Usage = "bb.showliveeditobjects"
                },
                new CheatItem
                {
                    Category = es ? "Construcción" : "Build",
                    Command = "bb.ignoregameplayunlocksentitlement",
                    Name = es ? "Desbloquear Objetos de Carrera" : "Unlock Career Objects",
                    Description = es
                        ? "Desbloquea objetos de recompensa de carrera en construcción/compra."
                        : "Unlocks career reward objects in build/buy.",
                    Usage = "bb.ignoregameplayunlocksentitlement"
                },
                new CheatItem
                {
                    Category = es ? "Construcción" : "Build",
                    Command = "bb.enablefreebuild",
                    Name = es ? "Construcción Libre" : "Free Build",
                    Description = es
                        ? "Permite editar lotes especiales como el hospital o comisaría."
                        : "Allows editing special lots like hospital or police station.",
                    Usage = "bb.enablefreebuild"
                },

                // CAS CHEATS
                new CheatItem
                {
                    Category = "CAS",
                    Command = "cas.fulleditmode",
                    Name = es ? "Modo Edición Completa CAS" : "Full Edit Mode CAS",
                    Description = es
                        ? "Permite edición completa en CAS (Crear un Sim) incluyendo cambiar rasgos y aspiraciones. Requiere testingcheats true. Shift+Click en el Sim y selecciona 'Modificar en CAS'."
                        : "Allows full editing in CAS (Create a Sim) including changing traits and aspirations. Requires testingcheats true. Shift+Click on Sim and select 'Modify in CAS'.",
                    Usage = "cas.fulleditmode"
                },

                // DEATH & LIFE CHEATS
                new CheatItem
                {
                    Category = es ? "Vida/Muerte" : "Life/Death",
                    Command = "death.toggle true/false",
                    Name = es ? "Desactivar Muerte" : "Disable Death",
                    Description = es
                        ? "Activa o desactiva la muerte en el juego."
                        : "Enables or disables death in the game.",
                    Usage = "death.toggle false (desactiva muerte / disables death)"
                },
                new CheatItem
                {
                    Category = es ? "Vida/Muerte" : "Life/Death",
                    Command = "traits.equip_trait Ghost_[tipo]",
                    Name = es ? "Convertir en Fantasma" : "Make Ghost",
                    Description = es
                        ? "Convierte al Sim en un fantasma. Tipos: OldAge, Drowning, Electrocution, Fire, Hunger, Anger, Embarrassment."
                        : "Turns the Sim into a ghost. Types: OldAge, Drowning, Electrocution, Fire, Hunger, Anger, Embarrassment.",
                    Usage = "traits.equip_trait Ghost_OldAge"
                },
                new CheatItem
                {
                    Category = es ? "Vida/Muerte" : "Life/Death",
                    Command = "traits.remove_trait Ghost_[tipo]",
                    Name = es ? "Quitar Fantasma" : "Remove Ghost",
                    Description = es
                        ? "Elimina el rasgo de fantasma del Sim."
                        : "Removes the ghost trait from the Sim.",
                    Usage = "traits.remove_trait Ghost_OldAge"
                },

                // ASPIRATION & SATISFACTION CHEATS
                new CheatItem
                {
                    Category = es ? "Aspiraciones" : "Aspirations",
                    Command = "aspirations.complete_current_milestone",
                    Name = es ? "Completar Hito Actual" : "Complete Current Milestone",
                    Description = es
                        ? "Completa el hito actual de la aspiración del Sim."
                        : "Completes the current milestone of the Sim's aspiration.",
                    Usage = "aspirations.complete_current_milestone"
                },
                new CheatItem
                {
                    Category = es ? "Aspiraciones" : "Aspirations",
                    Command = "sims.give_satisfaction_points [cantidad]",
                    Name = es ? "Puntos de Satisfacción" : "Satisfaction Points",
                    Description = es
                        ? "Añade puntos de satisfacción al Sim activo."
                        : "Adds satisfaction points to the active Sim.",
                    Usage = "sims.give_satisfaction_points 5000"
                },

                // TRAITS CHEATS
                new CheatItem
                {
                    Category = es ? "Rasgos" : "Traits",
                    Command = "traits.equip_trait [rasgo]",
                    Name = es ? "Añadir Rasgo" : "Add Trait",
                    Description = es
                        ? "Añade un rasgo al Sim. Ejemplos: Active, Cheerful, Creative, Genius, Romantic, etc."
                        : "Adds a trait to the Sim. Examples: Active, Cheerful, Creative, Genius, Romantic, etc.",
                    Usage = "traits.equip_trait Creative"
                },
                new CheatItem
                {
                    Category = es ? "Rasgos" : "Traits",
                    Command = "traits.remove_trait [rasgo]",
                    Name = es ? "Eliminar Rasgo" : "Remove Trait",
                    Description = es
                        ? "Elimina un rasgo del Sim."
                        : "Removes a trait from the Sim.",
                    Usage = "traits.remove_trait Creative"
                },

                // MISC CHEATS
                new CheatItem
                {
                    Category = es ? "Varios" : "Misc",
                    Command = "sims.hard_reset",
                    Name = es ? "Resetear Sim" : "Reset Sim",
                    Description = es
                        ? "Resetea al Sim seleccionado (útil si está atascado)."
                        : "Resets the selected Sim (useful if stuck).",
                    Usage = "sims.hard_reset"
                },
                new CheatItem
                {
                    Category = es ? "Varios" : "Misc",
                    Command = "sims.spawnsimple [ID]",
                    Name = es ? "Invocar Objeto" : "Spawn Object",
                    Description = es
                        ? "Invoca un objeto específico por su ID."
                        : "Spawns a specific object by its ID.",
                    Usage = "sims.spawnsimple"
                },
                new CheatItem
                {
                    Category = es ? "Varios" : "Misc",
                    Command = "clock.advance_game_time [horas] [minutos] [segundos]",
                    Name = es ? "Avanzar Tiempo" : "Advance Time",
                    Description = es
                        ? "Avanza el tiempo del juego."
                        : "Advances game time.",
                    Usage = "clock.advance_game_time 8 0 0"
                },
                new CheatItem
                {
                    Category = es ? "Varios" : "Misc",
                    Command = "sims.add_buff [buff]",
                    Name = es ? "Añadir Estado de Ánimo" : "Add Moodlet",
                    Description = es
                        ? "Añade un estado de ánimo específico. Ejemplos: e_buff_happy, e_buff_sad, e_buff_energized, e_buff_flirty, e_buff_angry."
                        : "Adds a specific moodlet. Examples: e_buff_happy, e_buff_sad, e_buff_energized, e_buff_flirty, e_buff_angry.",
                    Usage = "sims.add_buff e_buff_happy"
                },
                new CheatItem
                {
                    Category = es ? "Varios" : "Misc",
                    Command = "sims.remove_all_buffs",
                    Name = es ? "Eliminar Todos los Estados" : "Remove All Moodlets",
                    Description = es
                        ? "Elimina todos los estados de ánimo del Sim."
                        : "Removes all moodlets from the Sim.",
                    Usage = "sims.remove_all_buffs"
                }
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateCategoryButtons();
            DisplayCheats();
        }

        private void CreateCategoryButtons()
        {
            bool es = LanguageManager.IsSpanish;

            // Get unique categories
            var categories = _allCheats.Select(c => c.Category).Distinct().OrderBy(c => c).ToList();
            categories.Insert(0, es ? "Todos" : "All");

            // Add Favorites category
            categories.Insert(1, es ? "⭐ Favoritos" : "⭐ Favorites");

            foreach (var category in categories)
            {
                Button btn = new Button
                {
                    Content = category,
                    Style = (Style)FindResource("CategoryButton"),
                    Tag = category
                };

                if (category == (es ? "Todos" : "All"))
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1"));
                }

                btn.Click += CategoryButton_Click;
                CategoryPanel.Children.Add(btn);
            }
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;
            Button btn = sender as Button;
            _selectedCategory = btn.Tag.ToString();

            // Update button colors
            foreach (Button categoryBtn in CategoryPanel.Children)
            {
                if (categoryBtn.Tag.ToString() == _selectedCategory)
                {
                    categoryBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1"));
                }
                else
                {
                    categoryBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                }
            }

            DisplayCheats();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DisplayCheats();
        }

        private void DisplayCheats()
        {
            bool es = LanguageManager.IsSpanish;
            CheatsPanel.Children.Clear();

            string searchText = SearchBox.Text.ToLower();
            var filteredCheats = _allCheats.AsEnumerable();

            // Filter by category
            if (_selectedCategory == (es ? "⭐ Favoritos" : "⭐ Favorites"))
            {
                filteredCheats = filteredCheats.Where(c => _favoriteCommands.Contains(c.Command));
            }
            else if (_selectedCategory != "All" && _selectedCategory != "Todos")
            {
                filteredCheats = filteredCheats.Where(c => c.Category == _selectedCategory);
            }

            // Filter by search
            if (!string.IsNullOrWhiteSpace(searchText) && searchText != (es ? "buscar trucos..." : "search cheats..."))
            {
                filteredCheats = filteredCheats.Where(c =>
                    c.Command.ToLower().Contains(searchText) ||
                    c.Name.ToLower().Contains(searchText) ||
                    c.Description.ToLower().Contains(searchText));
            }

            foreach (var cheat in filteredCheats)
            {
                CreateCheatCard(cheat);
            }

            if (!filteredCheats.Any())
            {
                TextBlock noResults = new TextBlock
                {
                    Text = es ? "No se encontraron trucos" : "No cheats found",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    FontSize = 16,
                    FontFamily = new FontFamily("Bahnschrift Light"),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                CheatsPanel.Children.Add(noResults);
            }
        }

        private void CreateCheatCard(CheatItem cheat)
        {
            bool es = LanguageManager.IsSpanish;

            Border card = new Border
            {
                Style = (Style)FindResource("CheatCard")
            };

            Grid cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Info Panel
            StackPanel infoPanel = new StackPanel();

            // Category Badge
            Border categoryBadge = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6366F1")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 8)
            };
            TextBlock categoryText = new TextBlock
            {
                Text = cheat.Category,
                Foreground = Brushes.White,
                FontSize = 10,
                FontFamily = new FontFamily("Bahnschrift Light"),
                FontWeight = FontWeights.Bold
            };
            categoryBadge.Child = categoryText;
            infoPanel.Children.Add(categoryBadge);

            // Name
            TextBlock nameText = new TextBlock
            {
                Text = cheat.Name,
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Bahnschrift Light"),
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 0, 0, 8)
            };
            nameText.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                ShadowDepth = 1,
                Opacity = 0.8,
                BlurRadius = 6
            };
            infoPanel.Children.Add(nameText);

            // Command
            Border commandBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            TextBlock commandText = new TextBlock
            {
                Text = cheat.Command,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold
            };
            commandBorder.Child = commandText;
            infoPanel.Children.Add(commandBorder);

            // Description
            TextBlock descText = new TextBlock
            {
                Text = cheat.Description,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                FontSize = 13,
                FontFamily = new FontFamily("Bahnschrift Light"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            infoPanel.Children.Add(descText);

            // Usage
            TextBlock usageText = new TextBlock
            {
                Text = cheat.Usage,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                FontSize = 12,
                FontFamily = new FontFamily("Bahnschrift Light"),
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            infoPanel.Children.Add(usageText);

            Grid.SetColumn(infoPanel, 0);
            cardGrid.Children.Add(infoPanel);

            // Action Buttons Panel
            StackPanel actionPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10, 0, 0, 0)
            };

            // Favorite Button
            bool isFavorite = _favoriteCommands.Contains(cheat.Command);
            Button favoriteBtn = new Button
            {
                Content = isFavorite ? "⭐" : "☆",
                Style = (Style)FindResource("FavoriteButton"),
                Tag = cheat.Command,
                ToolTip = es ? "Marcar como favorito" : "Mark as favorite",
                Margin = new Thickness(0, 0, 0, 5)
            };

            if (isFavorite)
            {
                favoriteBtn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            }

            favoriteBtn.Click += FavoriteButton_Click;
            actionPanel.Children.Add(favoriteBtn);

            // Copy Button
            Button copyBtn = new Button
            {
                Content = "📋",
                Style = (Style)FindResource("CopyButton"),
                Tag = cheat.Command,
                ToolTip = es ? "Copiar al portapapeles" : "Copy to clipboard"
            };
            copyBtn.Click += CopyButton_Click;
            actionPanel.Children.Add(copyBtn);

            Grid.SetColumn(actionPanel, 1);
            cardGrid.Children.Add(actionPanel);

            card.Child = cardGrid;
            CheatsPanel.Children.Add(card);
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string command = btn.Tag.ToString();

            if (_favoriteCommands.Contains(command))
            {
                _favoriteCommands.Remove(command);
                btn.Content = "☆";
                btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            }
            else
            {
                _favoriteCommands.Add(command);
                btn.Content = "⭐";
                btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            }

            SaveFavorites();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            bool es = LanguageManager.IsSpanish;
            Button btn = sender as Button;
            string command = btn.Tag.ToString();

            try
            {
                System.Windows.Clipboard.SetText(command);
                btn.Content = "";

                // Reset after 2 seconds
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => btn.Content = "📋");
                });
            }
            catch
            {
                MessageBox.Show(
                    es ? "Error al copiar al portapapeles" : "Error copying to clipboard",
                    es ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportAllButton_Click(object sender, RoutedEventArgs e)
        {
            ExportCheats(_allCheats, "Sims4_AllCheats.txt");
        }

        private void ExportFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            var favoriteCheats = _allCheats.Where(c => _favoriteCommands.Contains(c.Command)).ToList();

            if (!favoriteCheats.Any())
            {
                bool es = LanguageManager.IsSpanish;
                MessageBox.Show(
                    es ? "No tienes trucos favoritos marcados." : "You don't have any favorite cheats marked.",
                    es ? "Sin Favoritos" : "No Favorites",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ExportCheats(favoriteCheats, "Sims4_FavoriteCheats.txt");
        }

        private void ExportCheats(List<CheatItem> cheats, string fileName)
        {
            bool es = LanguageManager.IsSpanish;

            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, fileName);

                StringBuilder sb = new StringBuilder();

                // Header
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("          THE SIMS 4 - CHEATS GUIDE");
                sb.AppendLine("          Generated by Leuan's ToolKit");
                sb.AppendLine($"          {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();
                sb.AppendLine($"Total Cheats: {cheats.Count}");
                sb.AppendLine();

                // Group by category
                var groupedCheats = cheats.GroupBy(c => c.Category).OrderBy(g => g.Key);

                foreach (var group in groupedCheats)
                {
                    sb.AppendLine();
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine($"  CATEGORY: {group.Key.ToUpper()}");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine();

                    foreach (var cheat in group)
                    {
                        sb.AppendLine($"┌─ {cheat.Name}");
                        sb.AppendLine($"│");
                        sb.AppendLine($"│  Command:");
                        sb.AppendLine($"│  → {cheat.Command}");
                        sb.AppendLine($"│");
                        sb.AppendLine($"│  Description:");
                        sb.AppendLine($"│  {WrapText(cheat.Description, 60)}");
                        sb.AppendLine($"│");
                        sb.AppendLine($"│  Usage:");
                        sb.AppendLine($"│  {WrapText(cheat.Usage, 60)}");
                        sb.AppendLine($"└───────────────────────────────────────────────────────────");
                        sb.AppendLine();
                    }
                }

                // Footer
                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("  HOW TO USE CHEATS:");
                sb.AppendLine("  1. Press Ctrl + Shift + C to open the cheat console");
                sb.AppendLine("  2. Type 'testingcheats true' and press Enter");
                sb.AppendLine("  3. Type your desired cheat and press Enter");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                MessageBox.Show(
                    es ? $"Archivo exportado exitosamente:\n{filePath}" : $"File exported successfully:\n{filePath}",
                    es ? "Exportación Exitosa" : "Export Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    es ? $"Error al exportar: {ex.Message}" : $"Error exporting: {ex.Message}",
                    es ? "Error" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string WrapText(string text, int maxWidth)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxWidth)
                return text;

            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > maxWidth)
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                }

                if (currentLine.Length > 0)
                    currentLine.Append(" ");
                currentLine.Append(word);
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());

            return string.Join("\n│  ", lines);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }

    // Clase auxiliar para los cheats
    public class CheatItem
    {
        public string Category { get; set; }
        public string Command { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Usage { get; set; }
    }
}