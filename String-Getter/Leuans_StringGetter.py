import os
import shutil
import customtkinter as ctk
from tkinter import filedialog, messagebox
from pathlib import Path
import subprocess

# Configuración visual
ctk.set_appearance_mode("Dark")
ctk.set_default_color_theme("blue")

class SimsStringsApp(ctk.CTk):
    def __init__(self):
        super().__init__()

        self.title("Sims 4 String Relocator")
        self.geometry("750x650")
        
        self.colors = {
            "primary": "#0ea5e9",
            "bg_dark": "#0f172a",
            "bg_card": "#1e293b",
            "text_secondary": "#94a3b8"
        }

        self.texts = {
            "EN": {
                "title": "Strings Relocator",
                "subtitle": "Extract and organize language files",
                "label_orig": "Game Installation Path",
                "label_dest": "Output Destination",
                "btn_browse": "Browse",
                "btn_start": "START EXTRACTION",
                "success": "Extraction completed!",
                "error_path": "The game path does not exist."
            },
            "ES": {
                "title": "Relocalizador de Strings",
                "subtitle": "Extrae y organiza archivos de idioma",
                "label_orig": "Ruta de Instalación del Juego",
                "label_dest": "Carpeta de Destino",
                "btn_browse": "Explorar",
                "btn_start": "INICIAR EXTRACCIÓN",
                "success": "¡Extracción completada!",
                "error_path": "La ruta del juego no existe o es inválida."
            }
        }
        self.current_lang = "ES"

        # Rutas dinámicas
        self.path_origin = ctk.StringVar(value=r"D:\SteamLibrary\steamapps\common\The Sims 4")
        self.path_dest = ctk.StringVar(value=str(Path.home() / "Desktop" / "Strings"))

        self.setup_ui()

    def setup_ui(self):
        # Limpiar interfaz anterior para el cambio de idioma
        for widget in self.winfo_children():
            widget.destroy()

        self.main_frame = ctk.CTkFrame(self, fg_color="transparent")
        self.main_frame.pack(fill="both", expand=True, padx=40, pady=30)

        # Header
        header = ctk.CTkFrame(self.main_frame, fg_color="transparent")
        header.pack(fill="x", pady=(0, 20))
        
        ctk.CTkLabel(header, text=self.texts[self.current_lang]["title"], font=("Segoe UI", 30, "bold")).pack(side="left")
        
        self.lang_btn = ctk.CTkSegmentedButton(header, values=["EN", "ES"], command=self.change_lang)
        self.lang_btn.set(self.current_lang)
        self.lang_btn.pack(side="right")

        # Inputs
        card = ctk.CTkFrame(self.main_frame, fg_color=self.colors["bg_card"], corner_radius=15)
        card.pack(fill="x", pady=10)
        
        inner = ctk.CTkFrame(card, fg_color="transparent")
        inner.pack(fill="x", padx=20, pady=20)

        self.add_field(inner, "label_orig", self.path_origin)
        self.add_field(inner, "label_dest", self.path_dest)

        # Consola
        self.log = ctk.CTkTextbox(self.main_frame, fg_color=self.colors["bg_dark"], font=("Consolas", 12))
        self.log.pack(fill="both", expand=True, pady=20)

        # Botón de Acción
        self.btn_run = ctk.CTkButton(
            self.main_frame, 
            text=self.texts[self.current_lang]["btn_start"],
            font=("Segoe UI", 16, "bold"),
            height=50,
            fg_color=self.colors["primary"],
            command=self.run_logic
        )
        self.btn_run.pack(fill="x")

    def add_field(self, parent, label_key, var):
        ctk.CTkLabel(parent, text=self.texts[self.current_lang][label_key], font=("Segoe UI", 12, "bold")).pack(anchor="w")
        row = ctk.CTkFrame(parent, fg_color="transparent")
        row.pack(fill="x", pady=(0, 10))
        ctk.CTkEntry(row, textvariable=var, height=35, fg_color=self.colors["bg_dark"]).pack(side="left", fill="x", expand=True, padx=(0, 10))
        ctk.CTkButton(row, text=self.texts[self.current_lang]["btn_browse"], width=80, command=lambda: self.pick(var)).pack(side="right")

    def pick(self, var):
        path = filedialog.askdirectory()
        if path: var.set(path)

    def change_lang(self, val):
        self.current_lang = val
        self.setup_ui()

    def run_logic(self):
        orig = Path(self.path_origin.get())
        dest = Path(self.path_dest.get())

        if not orig.exists():
            messagebox.showerror("Error", self.texts[self.current_lang]["error_path"])
            return

        self.btn_run.configure(state="disabled", text="...")
        self.log.delete("1.0", "end")
        
        try:
            client_path = orig / "Data" / "Client"
            idiomas = [f.stem.replace("Strings_", "") for f in client_path.glob("Strings_*.package")]

            if not idiomas:
                self.log.insert("end", "⚠️ No se detectaron idiomas en Data/Client.\n")
            
            for lang in idiomas:
                self.log.insert("end", f"▶ Procesando: {lang}\n")
                for folder in ["Data/Client", "Delta"]:
                    search_dir = orig / folder
                    if not search_dir.exists(): continue

                    for pkg in search_dir.rglob(f"Strings_{lang}.package"):
                        rel = pkg.relative_to(orig)
                        target = dest / lang / rel
                        target.parent.mkdir(parents=True, exist_ok=True)
                        shutil.copy2(pkg, target)
                        self.log.insert("end", f"  ✓ Copiado: {rel}\n")
                self.log.see("end")
            
            messagebox.showinfo("OK", self.texts[self.current_lang]["success"])
            # Abrir la carpeta al finalizar
            os.startfile(dest) if os.name == 'nt' else subprocess.call(['open', dest])

        except Exception as e:
            self.log.insert("end", f"❌ ERROR: {str(e)}\n")
        finally:
            self.btn_run.configure(state="normal", text=self.texts[self.current_lang]["btn_start"])

if __name__ == "__main__":
    app = SimsStringsApp()
    app.mainloop()
