using System.Xml.Linq;
using GestiónDeBiblioteca.Controlladores;
using GestiónDeBiblioteca.Model;
using GestiónDeBiblioteca.TDA;

namespace GestiónDeBiblioteca.Servicios
{
    /// <summary>
    /// Servicio para parsear archivos XML de entrada y cargar datos al catálogo.
    /// Soporta carga incremental: se pueden enviar múltiples archivos XML.
    /// Formato esperado:
    ///   &lt;config&gt;
    ///     &lt;listaCategorias&gt;
    ///       &lt;categoria padre="nombrePadre"&gt;nombre&lt;/categoria&gt;
    ///     &lt;/listaCategorias&gt;
    ///     &lt;listaLibros&gt;
    ///       &lt;libro&gt;
    ///         &lt;ISBN&gt;123&lt;/ISBN&gt;
    ///         &lt;titulo&gt;...&lt;/titulo&gt;
    ///         &lt;autor&gt;...&lt;/autor&gt;
    ///         &lt;categoria&gt;...&lt;/categoria&gt;
    ///       &lt;/libro&gt;
    ///     &lt;/listaLibros&gt;
    ///   &lt;/config&gt;
    /// </summary>
    public class XmlService
    {
        private readonly CatalogoController catalogo;

        public XmlService(CatalogoController catalogo)
        {
            this.catalogo = catalogo;
        }

        /// <summary>
        /// Resultado del procesamiento de un archivo XML.
        /// </summary>
        public class ResultadoCarga
        {
            public int CategoriasCreadas { get; set; }
            public int LibrosCargados { get; set; }
            public int LibrosDuplicados { get; set; }
            public ListaEnlazadaSimple<string> Errores { get; set; } = new ListaEnlazadaSimple<string>();
        }

        /// <summary>
        /// Procesa un archivo XML y carga los datos en el catálogo.
        /// Retorna un resultado con estadísticas de la carga.
        /// </summary>
        public ResultadoCarga ProcesarXml(Stream streamXml)
        {
            ResultadoCarga resultado = new ResultadoCarga();

            try
            {
                XDocument documento = XDocument.Load(streamXml);
                XElement? raiz = documento.Root;

                if (raiz == null)
                {
                    resultado.Errores.Agregar("El archivo XML no tiene un elemento raíz válido.");
                    return resultado;
                }

                // Procesar categorías primero (las categorías padre deben existir antes que las hijas)
                XElement? elemCategorias = raiz.Element("listaCategorias");

                if (elemCategorias != null)
                {
                    resultado.CategoriasCreadas = ProcesarCategorias(elemCategorias, resultado.Errores);
                }

                // Procesar libros
                XElement? elemLibros = raiz.Element("listaLibros");

                if (elemLibros != null)
                {
                    ProcesarLibros(elemLibros, resultado);
                }
            }
            catch (Exception ex)
            {
                resultado.Errores.Agregar("Error al procesar XML: " + ex.Message);
            }

            return resultado;
        }

        /// <summary>
        /// Procesa el elemento listaCategorias del XML.
        /// El atributo "padre" es opcional (categoría raíz si se omite).
        /// </summary>
        private int ProcesarCategorias(XElement elemCategorias, ListaEnlazadaSimple<string> errores)
        {
            int creadas = 0;

            // Primera pasada: categorías raíz (sin padre)
            foreach (XElement elem in elemCategorias.Elements("categoria"))
            {
                string? padre = elem.Attribute("padre")?.Value;
                string nombre = elem.Value.Trim();

                if (string.IsNullOrEmpty(nombre))
                {
                    continue;
                }

                if (padre == null || string.IsNullOrEmpty(padre.Trim()))
                {
                    // Categoría raíz
                    catalogo.AgregarCategoria(nombre);
                    creadas++;
                }
            }

            // Segunda pasada: categorías con padre
            bool hayCambios = true;
            int iteraciones = 0;
            int totalElementos = elemCategorias.Elements("categoria").Count();

            while (hayCambios && iteraciones < totalElementos)
            {
                hayCambios = false;
                iteraciones++;

                foreach (XElement elem in elemCategorias.Elements("categoria"))
                {
                    string? padre = elem.Attribute("padre")?.Value;
                    string nombre = elem.Value.Trim();

                    if (string.IsNullOrEmpty(nombre) || padre == null || string.IsNullOrEmpty(padre.Trim()))
                    {
                        continue;
                    }

                    // Verificar si la categoría ya existe
                    if (catalogo.BuscarCategoria(padre.Trim() + " > " + nombre) != null)
                    {
                        continue;
                    }

                    // Verificar si el padre ya existe
                    Categoria? padreExistente = catalogo.BuscarCategoria(padre.Trim());

                    if (padreExistente != null)
                    {
                        catalogo.AgregarSubcategoria(padre.Trim(), nombre);
                        creadas++;
                        hayCambios = true;
                    }
                }
            }

            return creadas;
        }

        /// <summary>
        /// Procesa el elemento listaLibros del XML.
        /// Cada libro debe tener ISBN, título, autor y categoría.
        /// </summary>
        private void ProcesarLibros(XElement elemLibros, ResultadoCarga resultado)
        {
            foreach (XElement elem in elemLibros.Elements("libro"))
            {
                try
                {
                    string strISBN = elem.Element("ISBN")?.Value?.Trim() ?? "";
                    string titulo = elem.Element("titulo")?.Value?.Trim() ?? "";
                    string autor = elem.Element("autor")?.Value?.Trim() ?? "";
                    string categoria = elem.Element("categoria")?.Value?.Trim() ?? "";

                    // Validaciones
                    if (string.IsNullOrEmpty(strISBN) || !int.TryParse(strISBN, out int isbn))
                    {
                        resultado.Errores.Agregar($"ISBN inválido en libro: '{strISBN}'");
                        continue;
                    }

                    if (string.IsNullOrEmpty(titulo))
                    {
                        resultado.Errores.Agregar($"Libro ISBN {isbn}: título vacío");
                        continue;
                    }

                    if (string.IsNullOrEmpty(autor))
                    {
                        resultado.Errores.Agregar($"Libro ISBN {isbn}: autor vacío");
                        continue;
                    }

                    if (string.IsNullOrEmpty(categoria))
                    {
                        resultado.Errores.Agregar($"Libro ISBN {isbn}: categoría vacía");
                        continue;
                    }

                    // Crear el libro
                    Libro libro = new Libro(isbn, titulo, autor, categoria);

                    // Intentar registrar
                    bool registrado = catalogo.RegistrarLibro(libro);

                    if (registrado)
                    {
                        resultado.LibrosCargados++;
                    }
                    else
                    {
                        resultado.LibrosDuplicados++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.Errores.Agregar($"Error procesando libro: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Genera un XML de ejemplo con datos de prueba.
        /// </summary>
        public static string GenerarXmlEjemplo()
        {
            return @"<?xml version=""1.0""?>
<config>
  <listaCategorias>
    <categoria>Ficción</categoria>
    <categoria>Ciencia</categoria>
    <categoria>Tecnología</categoria>
    <categoria>Historia</categoria>
    <categoria=""padre""=""Ficción"">Ciencia Ficción</categoria>
    <categoria padre=""Ficción"">Fantasía</categoria>
    <categoria padre=""Ciencia"">Física</categoria>
    <categoria padre=""Ciencia"">Biología</categoria>
    <categoria padre=""Tecnología"">Programación</categoria>
    <categoria padre=""Tecnología"">Redes</categoria>
    <categoria padre=""Historia"">Antigua</categoria>
    <categoria padre=""Historia"">Moderna</categoria>
  </listaCategorias>
  <listaLibros>
    <libro>
      <ISBN>1001</ISBN>
      <titulo>El Señor de los Anillos</titulo>
      <autor>J.R.R. Tolkien</autor>
      <categoria>Fantasía</categoria>
    </libro>
    <libro>
      <ISBN>1002</ISBN>
      <titulo>Dune</titulo>
      <autor>Frank Herbert</autor>
      <categoria>Ciencia Ficción</categoria>
    </libro>
    <libro>
      <ISBN>1003</ISBN>
      <titulo>1984</titulo>
      <autor>George Orwell</autor>
      <categoria>Ciencia Ficción</categoria>
    </libro>
    <libro>
      <ISBN>1004</ISBN>
      <titulo>Breve historia del tiempo</titulo>
      <autor>Stephen Hawking</autor>
      <categoria>Física</categoria>
    </libro>
    <libro>
      <ISBN>1005</ISBN>
      <titulo>El gen egoísta</titulo>
      <autor>Richard Dawkins</autor>
      <categoria>Biología</categoria>
    </libro>
    <libro>
      <ISBN>1006</ISBN>
      <titulo>Clean Code</titulo>
      <autor>Robert C. Martin</autor>
      <categoria>Programación</categoria>
    </libro>
    <libro>
      <ISBN>1007</ISBN>
      <titulo>Redes de computadoras</titulo>
      <autor>Andrew Tanenbaum</autor>
      <categoria>Redes</categoria>
    </libro>
    <libro>
      <ISBN>1008</ISBN>
      <titulo>Historia de Roma</titulo>
      <autor>Theodore Mommsen</autor>
      <categoria>Antigua</categoria>
    </libro>
    <libro>
      <ISBN>1009</ISBN>
      <titulo>La Segunda Guerra Mundial</titulo>
      <autor>Antony Beevor</autor>
      <categoria>Moderna</categoria>
    </libro>
    <libro>
      <ISBN>1010</ISBN>
      <titulo>Design Patterns</titulo>
      <autor>GoF</autor>
      <categoria>Programación</categoria>
    </libro>
  </listaLibros>
</config>";
        }
    }
}
