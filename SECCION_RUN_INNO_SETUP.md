# 🚀 Guía de la Sección [Run] en Inno Setup

## 🎯 Problema Resuelto

**Error original:**
```
Line 44: Unrecognized parameter name 'Name'
```

**Línea incorrecta:**
```ini
Name: "{app}\{#MyAppExeName}"; Description: "..."; Flags: nowait postinstall skipifsilent
```

**Causa:** En la sección `[Run]`, el parámetro correcto es **`Filename`**, NO `Name`

**Solución aplicada:**
```ini
Filename: "{app}\{#MyAppExeName}"; Description: "..."; Flags: nowait postinstall skipifsilent
```

---

## 📚 Diferencias entre Secciones

Cada sección de Inno Setup tiene **parámetros diferentes**:

### **Sección [Icons] - Usa `Name:`**
```ini
[Icons]
Name: "{group}\MAGNOR POS"; Filename: "{app}\MAGNOR_POS.exe"
Name: "{autodesktop}\MAGNOR POS"; Filename: "{app}\MAGNOR_POS.exe"
```
- **Name:** Nombre y ubicación del acceso directo
- **Filename:** Ruta del ejecutable al que apunta

### **Sección [Run] - Usa `Filename:`**
```ini
[Run]
Filename: "{app}\MAGNOR_POS.exe"; Description: "Ejecutar MAGNOR POS"; Flags: nowait postinstall skipifsilent
```
- **Filename:** Programa a ejecutar
- **Description:** Texto que ve el usuario
- **Flags:** Comportamiento de la ejecución

### **Sección [Files] - Usa `Source:` y `DestDir:`**
```ini
[Files]
Source: "MAGNOR_POS\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion
```

---

## 🔧 Anatomía de [Run]

La sección `[Run]` ejecuta programas **durante o después** de la instalación.

### **Parámetros Obligatorios:**

| Parámetro | Descripción | Ejemplo |
|-----------|-------------|---------|
| **Filename** | Ruta del programa a ejecutar | `"{app}\MiApp.exe"` |

### **Parámetros Opcionales Comunes:**

| Parámetro | Descripción | Ejemplo |
|-----------|-------------|---------|
| **Description** | Texto mostrado al usuario | `"Ejecutar MAGNOR POS ahora"` |
| **Parameters** | Argumentos de línea de comandos | `"/config setup.ini"` |
| **WorkingDir** | Directorio de trabajo | `"{app}"` |
| **Flags** | Opciones de comportamiento | `postinstall nowait` |
| **StatusMsg** | Mensaje durante la instalación | `"Configurando base de datos..."` |

---

## 🚩 Flags Importantes

Los **Flags** controlan **cuándo y cómo** se ejecuta el programa:

### **Flags de Tiempo:**

| Flag | Descripción | Cuándo usarlo |
|------|-------------|---------------|
| `postinstall` | Ejecuta **después** de instalar archivos | Abrir app al terminar instalación ✅ |
| (sin flag) | Ejecuta **durante** la instalación | Scripts de configuración |

### **Flags de Interacción:**

| Flag | Descripción | Cuándo usarlo |
|------|-------------|---------------|
| `skipifsilent` | NO ejecuta si instalación es silenciosa | Evitar abrir app en instalaciones automáticas ✅ |
| `nowait` | NO espera a que termine el programa | Abrir app y terminar instalador ✅ |
| `waituntilterminated` | Espera a que termine el programa | Scripts que deben completarse antes de continuar |
| `runhidden` | Ejecuta oculto (sin ventana) | Scripts en segundo plano |

### **Flags de Permisos:**

| Flag | Descripción | Cuándo usarlo |
|------|-------------|---------------|
| `runasoriginaluser` | Ejecuta con permisos del usuario, NO admin | Abrir app después de instalar como admin ✅ |

---

## ✅ Ejemplos Correctos

### **1. Ejecutar la App Después de Instalar (Tu Caso)**
```ini
[Run]
Filename: "{app}\MAGNOR_POS.exe"; Description: "Ejecutar MAGNOR POS"; Flags: nowait postinstall skipifsilent
```

**Qué hace:**
- ✅ Pregunta al usuario si quiere abrir la app al terminar
- ✅ NO espera a que cierre la app (nowait)
- ✅ NO abre la app si instalación es silenciosa (skipifsilent)

### **2. Ejecutar Script de Configuración**
```ini
[Run]
Filename: "{app}\setup_database.bat"; StatusMsg: "Configurando base de datos..."; Flags: runhidden waituntilterminated
```

**Qué hace:**
- ✅ Ejecuta script durante la instalación
- ✅ Lo ejecuta oculto (runhidden)
- ✅ Espera a que termine antes de continuar (waituntilterminated)

### **3. Abrir Documentación en el Navegador**
```ini
[Run]
Filename: "https://docs.magnorpos.com/inicio"; Description: "Ver documentación"; Flags: postinstall shellexec skipifsilent
```

**Qué hace:**
- ✅ Abre URL en el navegador predeterminado (shellexec)
- ✅ Pregunta al usuario después de instalar (postinstall)

### **4. Instalar Prerequisitos (.NET Runtime)**
```ini
[Run]
Filename: "{tmp}\dotnet-runtime-8.0-installer.exe"; Parameters: "/quiet /norestart"; StatusMsg: "Instalando .NET 8..."; Flags: waituntilterminated
```

**Qué hace:**
- ✅ Instala .NET 8 Runtime automáticamente
- ✅ Espera a que termine la instalación (waituntilterminated)
- ✅ Instalación silenciosa (/quiet)

---

## 🎨 Personalizar el Texto del Checkbox

La **Description** se muestra en un checkbox al final de la instalación:

### **Método 1: Texto Directo (Simple)**
```ini
Filename: "{app}\MAGNOR_POS.exe"; Description: "Ejecutar MAGNOR POS ahora"; Flags: nowait postinstall skipifsilent
```

### **Método 2: Texto Localizado (Profesional)**
```ini
Filename: "{app}\MAGNOR_POS.exe"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
```

**¿Qué hace `{cm:LaunchProgram,...}`?**
- `cm` = Custom Message (mensaje personalizado)
- `LaunchProgram` = Cadena predefinida de Inno Setup
- Se traduce automáticamente según el idioma de instalación:
  - Español: "Ejecutar MAGNOR POS"
  - Inglés: "Launch MAGNOR POS"

**¿Qué hace `StringChange(MyAppName, '&', '&&')`?**
- Escapa el carácter `&` en el nombre de la app
- Evita que se interprete como atajo de teclado

---

## 🧪 Casos de Uso Comunes

### **1. Solo Abrir la App**
```ini
[Run]
Filename: "{app}\MiApp.exe"; Flags: nowait postinstall skipifsilent
```

### **2. Abrir App + Archivo de Configuración**
```ini
[Run]
Filename: "{app}\MiApp.exe"; Parameters: "--first-run"; Flags: nowait postinstall skipifsilent
```

### **3. Ejecutar Script de Instalación de Base de Datos**
```ini
[Run]
Filename: "{app}\install_db.bat"; WorkingDir: "{app}"; Flags: runhidden waituntilterminated
```

### **4. Mostrar README al Terminar**
```ini
[Run]
Filename: "{app}\README.txt"; Description: "Ver notas de la versión"; Flags: postinstall shellexec skipifsilent
```

### **5. Instalar Drivers (Requiere Admin)**
```ini
[Run]
Filename: "{app}\drivers\install_driver.exe"; Parameters: "/S"; StatusMsg: "Instalando controladores..."; Flags: waituntilterminated
```

---

## ✅ Script Corregido Completo

Tu sección `[Run]` ahora es:

```ini
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
```

**Parámetros usados:**
- ✅ **Filename:** `{app}\MAGNOR_POS.exe` - Ejecutable a abrir
- ✅ **Description:** Texto localizado "Ejecutar MAGNOR POS"
- ✅ **nowait:** No espera a que cierre la app
- ✅ **postinstall:** Pregunta al usuario al terminar instalación
- ✅ **skipifsilent:** No abre si instalación es silenciosa

---

## 🎯 Compilar Ahora

Con la corrección aplicada:

1. **Abre Inno Setup Compiler**
2. **Carga** `installer.iss`
3. **Compila** (F9)
4. **Resultado:** `Installer\MAGNOR_POS_Setup.exe` ✅

¡El error está resuelto! 🎉
