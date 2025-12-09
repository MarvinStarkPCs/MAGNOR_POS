# Assets/Images

Esta carpeta contiene los recursos gráficos de la aplicación MAGNOR_POS.

## Estructura Recomendada

```
Assets/
├── Images/
│   ├── logo.png          # Logo principal de la aplicación
│   ├── logo-small.png    # Logo pequeño para barra de título
│   ├── icon.ico          # Icono de la aplicación
│   └── login-bg.jpg      # Imagen de fondo para pantalla de login (opcional)
```

## Formatos Recomendados

- **Logo principal**: PNG con transparencia, tamaño recomendado 512x512px
- **Logo pequeño**: PNG con transparencia, 64x64px o 128x128px
- **Icono de aplicación**: ICO con múltiples tamaños (16x16, 32x32, 48x48, 256x256)
- **Fondos**: JPG o PNG, resolución HD (1920x1080) o superior

## Cómo Usar los Logos en XAML

### Logo en Login Window
```xml
<Image Source="/Assets/Images/logo.png"
       Width="200"
       Height="200"
       Margin="0,20,0,20"/>
```

### Logo en MainWindow (barra de título o header)
```xml
<Image Source="/Assets/Images/logo-small.png"
       Width="32"
       Height="32"/>
```

### Icono de aplicación en ventana
En el archivo .csproj, agregar:
```xml
<ApplicationIcon>Assets\Images\icon.ico</ApplicationIcon>
```

O en XAML de cada ventana:
```xml
<Window Icon="/Assets/Images/icon.ico">
```

## Nota Importante

Una vez que coloques los archivos de imagen en esta carpeta, asegúrate de que estén incluidos en el proyecto:
1. En Visual Studio: Botón derecho en el archivo → Propiedades → Build Action: "Resource"
2. O agregar en MAGNOR_POS.csproj:
```xml
<ItemGroup>
  <Resource Include="Assets\Images\**\*" />
</ItemGroup>
```
