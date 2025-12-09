# Cómo Crear el Instalador de MAGNOR POS

## Opción 1: Inno Setup (Recomendado - Fácil y Gratuito)

### Paso 1: Descargar Inno Setup
1. Ve a https://jrsoftware.org/isinfo.php
2. Descarga **Inno Setup 6** (última versión)
3. Instala Inno Setup en tu computadora

### Paso 2: Compilar la Aplicación en Release
```bash
cd C:\Users\MarvinStarkPCs\source\repos\MAGNOR_POS\MAGNOR_POS
dotnet publish -c Release -r win-x64 --self-contained false
```

### Paso 3: Crear el Instalador
1. Abre Inno Setup Compiler
2. Abre el archivo `installer.iss` (en la raíz del proyecto)
3. Presiona F9 o ve a **Build → Compile**
4. El instalador se creará en la carpeta `Installer\MAGNOR_POS_Setup.exe`

### Características del Instalador:
- ✅ Instalación en Archivos de Programa
- ✅ Icono en el escritorio (opcional)
- ✅ Acceso directo en el menú Inicio
- ✅ Desinstalador incluido
- ✅ Limpia la base de datos al desinstalar
- ✅ Interfaz en español

---

## Opción 2: ClickOnce (Desde Visual Studio)

### Paso 1: Abrir en Visual Studio
1. Abre `MAGNOR_POS.sln` en Visual Studio 2022
2. Haz clic derecho en el proyecto MAGNOR_POS

### Paso 2: Publicar
1. Selecciona **Publicar...**
2. Elige **Carpeta** como destino
3. Selecciona una ubicación (ej: `C:\Publish\MAGNOR_POS`)
4. Click en **Publicar**

### Paso 3: Distribuir
- Los archivos publicados estarán en la carpeta seleccionada
- Puedes comprimirlos en ZIP o crear un instalador con Inno Setup

---

## Opción 3: Publicación Autocontenida (Sin .NET instalado)

Si quieres que funcione sin tener .NET instalado:

```bash
cd C:\Users\MarvinStarkPCs\source\repos\MAGNOR_POS\MAGNOR_POS
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Esto crea un solo ejecutable (.exe) que incluye todo lo necesario.

**Ventajas:**
- ✅ No requiere .NET instalado
- ✅ Un solo archivo ejecutable
- ❌ Archivo más grande (~70-100 MB)

---

## Recomendación Final

**Para distribución profesional:**
1. Usa `dotnet publish` con `--self-contained true`
2. Crea el instalador con **Inno Setup**
3. El instalador resultante será fácil de distribuir

**Para pruebas rápidas:**
1. Usa `dotnet publish` sin self-contained
2. Comprime la carpeta en ZIP
3. El usuario necesitará .NET 8 Desktop Runtime instalado

---

## Archivos Generados

Después de seguir los pasos, tendrás:
- `Installer\MAGNOR_POS_Setup.exe` - Instalador ejecutable
- Tamaño aproximado: 5-10 MB (sin self-contained) o 70-100 MB (con self-contained)
