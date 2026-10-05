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
            public ListaSimpleCadenas Errores { get; set; } = new ListaSimpleCadenas();
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
        /// Soporta orden arbitrario (linking diferido): un hijo puede aparecer
        /// antes que su padre; se resuelve por pasadas iterativas.
        /// Unicidad global por nombre: duplicados intra-archivo se reportan,
        /// reutilización entre archivos (carga incremental) se acepta en silencio.
        /// Si el padre nunca aparece, la categoría se rechaza (no se crean fantasmas).
        /// </summary>
        private int ProcesarCategorias(XElement elemCategorias, ListaSimpleCadenas errores)
        {
            int creadas = 0;

            // Materializar a arreglos nativos (sin LINQ/List)
            int n = 0;
            foreach (XElement e in elemCategorias.Elements("categoria"))
            {
                n++;
            }

            if (n == 0)
            {
                return 0;
            }

            XElement[] elems = new XElement[n];
            int idx = 0;
            foreach (XElement e in elemCategorias.Elements("categoria"))
            {
                elems[idx] = e;
                idx++;
            }

            string[] nombres = new string[n];
            string[] padres = new string[n];
            bool[] omitir = new bool[n];

            for (int i = 0; i < n; i++)
            {
                string nombre = elems[i].Value.Trim();
                string padre = elems[i].Attribute("padre")?.Value?.Trim() ?? "";
                nombres[i] = nombre;
                padres[i] = padre;
                if (string.IsNullOrEmpty(nombre))
                {
                    omitir[i] = true;
                }
            }

            // Duplicados dentro del mismo archivo (insensible a mayúsculas)
            for (int i = 0; i < n; i++)
            {
                if (omitir[i])
                {
                    continue;
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(nombres[j], nombres[i], StringComparison.OrdinalIgnoreCase))
                    {
                        errores.Agregar($"Categoría '{nombres[i]}' duplicada en el archivo, se omite.");
                        omitir[i] = true;
                        break;
                    }
                }
            }

            // Primera pasada: raíces (sin padre) — orden independiente
            for (int i = 0; i < n; i++)
            {
                if (omitir[i] || !string.IsNullOrEmpty(padres[i]))
                {
                    continue;
                }

                if (catalogo.BuscarCategoriaPorNombre(nombres[i]) == null)
                {
                    catalogo.AgregarCategoria(nombres[i]);
                    creadas++;
                }
                // Si ya existía de una carga previa, se reutiliza en silencio.
            }

            // Pasadas iterativas: hijos cuyo padre ya existe (cadenas profundas)
            bool hayCambios = true;
            int iteraciones = 0;

            while (hayCambios && iteraciones < n + 1)
            {
                hayCambios = false;
                iteraciones++;

                for (int i = 0; i < n; i++)
                {
                    if (omitir[i] || string.IsNullOrEmpty(padres[i]))
                    {
                        continue;
                    }

                    // Ya existe (creada en este archivo o en carga previa): no contar de nuevo
                    if (catalogo.BuscarCategoriaPorNombre(nombres[i]) != null)
                    {
                        continue;
                    }

                    string nombrePadre = padres[i];
                    Categoria? padreExistente = catalogo.BuscarCategoria(nombrePadre);

                    if (padreExistente == null)
                    {
                        padreExistente = catalogo.BuscarCategoriaPorNombre(nombrePadre);
                    }

                    if (padreExistente != null)
                    {
                        string rutaPadreReal = padreExistente.ObtenerRutaCompleta();
                        const string prefijoRaiz = "Biblioteca > ";
                        if (rutaPadreReal.StartsWith(prefijoRaiz))
                        {
                            rutaPadreReal = rutaPadreReal.Substring(prefijoRaiz.Length);
                        }
                        else if (rutaPadreReal == "Biblioteca")
                        {
                            rutaPadreReal = nombrePadre;
                        }

                        Categoria? creadaCat = catalogo.AgregarSubcategoria(rutaPadreReal, nombres[i]);

                        if (creadaCat != null)
                        {
                            creadas++;
                            hayCambios = true;
                        }
                    }
                }
            }

            // Huérfanas: padre nunca declarado -> rechazo explícito
            for (int i = 0; i < n; i++)
            {
                if (omitir[i] || string.IsNullOrEmpty(padres[i]))
                {
                    continue;
                }

                if (catalogo.BuscarCategoriaPorNombre(nombres[i]) == null)
                {
                    errores.Agregar($"Categoría '{nombres[i]}': padre '{padres[i]}' no encontrado, se rechaza.");
                }
            }

            return creadas;
        }

        /// <summary>
        /// Procesa el elemento listaLibros del XML.
        /// Cada libro debe tener ISBN (int válido, no duplicado), título,
        /// autor y categoría existente (no se crean categorías fantasmas).
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
                        resultado.Errores.Agregar($"ISBN inválido en libro: '{strISBN}', se rechaza.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(titulo))
                    {
                        resultado.Errores.Agregar($"Libro ISBN {isbn}: título vacío, se rechaza.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(autor))
                    {
                        resultado.Errores.Agregar($"Libro ISBN {isbn}: autor vacío, se rechaza.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(categoria))
                    {
                        resultado.Errores.Agregar($"Libro ISBN {isbn}: categoría vacía, se rechaza.");
                        continue;
                    }

                    // La categoría debe existir (padres + subcategorías ya cargadas)
                    Categoria? catExistente = catalogo.BuscarCategoria(categoria);

                    if (catExistente == null)
                    {
                        catExistente = catalogo.BuscarCategoriaPorNombre(categoria);
                    }

                    if (catExistente == null)
                    {
                        resultado.Errores.Agregar($"Libro ISBN {isbn}: categoría '{categoria}' no existe, se rechaza.");
                        continue;
                    }

                    // ISBN duplicado (en este archivo o en cargas previas)
                    if (catalogo.BuscarPorISBN(isbn) != null)
                    {
                        resultado.LibrosDuplicados++;
                        resultado.Errores.Agregar($"Libro ISBN {isbn} duplicado, se omite.");
                        continue;
                    }

                    // Crear el libro con la ruta real para jerarquía profunda
                    string rutaCategoriaReal = catExistente.ObtenerRutaCompleta();
                    Libro libro = new Libro(isbn, titulo, autor, rutaCategoriaReal);

                    // Intentar registrar
                    bool registrado = catalogo.RegistrarLibro(libro);

                    if (registrado)
                    {
                        resultado.LibrosCargados++;
                    }
                    else
                    {
                        resultado.LibrosDuplicados++;
                        resultado.Errores.Agregar($"Libro ISBN {isbn} no se pudo registrar, se omite.");
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
    <categoria padre=""Ficción"">Ciencia Ficción</categoria>
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
