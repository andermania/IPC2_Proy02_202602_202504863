using GestiónDeBiblioteca.Controlladores;
using GestiónDeBiblioteca.Servicios;
using GestiónDeBiblioteca.TDA;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestiónDeBiblioteca.Pages
{
    public class IndexModel : PageModel
    {
        private readonly CatalogoController catalogo;
        private readonly IWebHostEnvironment entorno;

        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }
        public string[] ResultadosCarga { get; set; } = new string[0];
        public string[] ErroresCarga { get; set; } = new string[0];
        public int TotalLibros { get; set; }
        public int TotalCategorias { get; set; }

        public IndexModel(CatalogoController catalogo, IWebHostEnvironment entorno)
        {
            this.catalogo = catalogo;
            this.entorno = entorno;
        }

        public void OnGet()
        {
            TotalLibros = catalogo.ObtenerTotalLibros();
            TotalCategorias = catalogo.ObtenerTotalCategorias();
        }

        /// <summary>
        /// Inicialización: deja el sistema sin ninguna información previa.
        /// </summary>
        public IActionResult OnPostReiniciar()
        {
            catalogo.Reiniciar();
            MensajeExito = "Sistema inicializado: catálogo vacío listo para cargar datos.";
            TotalLibros = 0;
            TotalCategorias = catalogo.ObtenerTotalCategorias();
            ResultadosCarga = new string[0];
            ErroresCarga = new string[0];
            return Page();
        }

        /// <summary>
        /// Carga todos los XML de la carpeta XML/ del proyecto (carga incremental).
        /// No navega a otra página: muestra mensajes inline por archivo.
        /// Éxito: "Libros Cargados (archivo: X libros, Y categorías)".
        /// Fallo: "Error al Cargar archivo: nombre - motivo".
        /// </summary>
        public IActionResult OnPostCargarCarpeta()
        {
            ListaSimpleCadenas exitos = new ListaSimpleCadenas();
            ListaSimpleCadenas errores = new ListaSimpleCadenas();
            XmlService xmlService = new XmlService(catalogo);

            string carpeta = Path.Combine(entorno.ContentRootPath, "XML");

            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
                MensajeError = "No hay archivos .xml en la carpeta XML/. Coloque sus archivos allí y vuelva a pulsar Cargar Libros.";
                TotalLibros = catalogo.ObtenerTotalLibros();
                TotalCategorias = catalogo.ObtenerTotalCategorias();
                return Page();
            }

            string[] archivos = Directory.GetFiles(carpeta, "*.xml");
            OrdenarAscendente(archivos);

            if (archivos.Length == 0)
            {
                MensajeError = "No hay archivos .xml en la carpeta XML/. Coloque sus archivos allí y vuelva a pulsar Cargar Libros.";
                TotalLibros = catalogo.ObtenerTotalLibros();
                TotalCategorias = catalogo.ObtenerTotalCategorias();
                return Page();
            }

            int totalLibros = 0;
            int totalCategorias = 0;

            for (int i = 0; i < archivos.Length; i++)
            {
                string nombre = Path.GetFileName(archivos[i]);

                try
                {
                    XmlService.ResultadoCarga resultado;
                    using (FileStream fs = System.IO.File.OpenRead(archivos[i]))
                    {
                        resultado = xmlService.ProcesarXml(fs);
                    }

                    if (resultado.LibrosCargados > 0 || resultado.CategoriasCreadas > 0)
                    {
                        totalLibros += resultado.LibrosCargados;
                        totalCategorias += resultado.CategoriasCreadas;
                        exitos.Agregar($"Libros Cargados ({nombre}: {resultado.LibrosCargados} libros, {resultado.CategoriasCreadas} categorías)");
                    }
                    else if (resultado.Errores.ObtenerCantidad() > 0)
                    {
                        string[] detalle = resultado.Errores.ObtenerArray();
                        errores.Agregar($"Error al Cargar archivo: {nombre} - {detalle[0]}");
                    }
                    else
                    {
                        errores.Agregar($"Error al Cargar archivo: {nombre} - sin datos válidos");
                    }
                }
                catch (Exception ex)
                {
                    errores.Agregar($"Error al Cargar archivo: {nombre} - {ex.Message}");
                }
            }

            ResultadosCarga = exitos.ObtenerArray();
            ErroresCarga = errores.ObtenerArray();
            TotalLibros = catalogo.ObtenerTotalLibros();
            TotalCategorias = catalogo.ObtenerTotalCategorias();

            if (ResultadosCarga.Length > 0 && ErroresCarga.Length == 0)
            {
                MensajeExito = $"Carga completada: {totalLibros} libros y {totalCategorias} categorías desde {ResultadosCarga.Length} archivo(s).";
            }
            else if (ResultadosCarga.Length > 0)
            {
                MensajeExito = $"Carga parcial: {totalLibros} libros y {totalCategorias} categorías desde {ResultadosCarga.Length} archivo(s).";
                MensajeError = $"Se encontraron errores en {ErroresCarga.Length} archivo(s). Revise el detalle.";
            }
            else
            {
                MensajeError = "No se cargaron datos. Revise el detalle por archivo.";
            }

            return Page();
        }

        /// <summary>
        /// Ordenamiento por inserción propio (sin LINQ) para orden alfabético determinista.
        /// </summary>
        private void OrdenarAscendente(string[] archivos)
        {
            for (int i = 1; i < archivos.Length; i++)
            {
                string actual = archivos[i];
                int j = i - 1;
                while (j >= 0 && string.Compare(archivos[j], actual, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    archivos[j + 1] = archivos[j];
                    j--;
                }
                archivos[j + 1] = actual;
            }
        }
    }
}
