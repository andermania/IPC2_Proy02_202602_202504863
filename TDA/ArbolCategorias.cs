using GestiónDeBiblioteca.Model;

namespace GestiónDeBiblioteca.TDA
{
    /// <summary>
    /// Árbol N-ario que representa la jerarquía de categorías de la biblioteca.
    /// Soporta profundidad ilimitada (categorías → subcategorías → sub-subcategorías...).
    /// Cada nodo categoría contiene un AVL de libros asociados.
    /// Las subcategorías se mantienen ordenadas alfabéticamente.
    /// </summary>
    public class ArbolCategorias
    {
        private Categoria raiz;
        private int totalCategorias;

        public ArbolCategorias()
        {
            // La raíz es una categoría virtual que agrupa todo
            raiz = new Categoria("Biblioteca");
            totalCategorias = 1;
        }

        /// <summary>
        /// Retorna la categoría raíz del árbol.
        /// </summary>
        public Categoria ObtenerRaiz()
        {
            return raiz;
        }

        /// <summary>
        /// Agrega una categoría como hija directa de la raíz.
        /// Los nombres son globalmente únicos: si ya existe en cualquier nivel, retorna la existente.
        /// </summary>
        public Categoria AgregarCategoria(string nombre)
        {
            Categoria? global = BuscarPorNombreGlobal(nombre);

            if (global != null)
            {
                return global;
            }

            Categoria nueva = new Categoria(nombre, raiz);
            raiz.Hijos.InsertarOrdenado(nueva);
            totalCategorias++;
            return nueva;
        }

        /// <summary>
        /// Agrega una subcategoría bajo un padre específico.
        /// La ruta del padre se indica con nombres separados por '>' (ej: "Ciencia > Física").
        /// El padre se resuelve por ruta y, si falla, por nombre global (nombres únicos).
        /// Si el nombre hija ya existe en cualquier nivel, retorna la existente (unicidad global).
        /// Si el padre no existe, se crean las categorías intermedias.
        /// </summary>
        public Categoria AgregarSubcategoria(string rutaPadre, string nombreHija)
        {
            Categoria? globalHija = BuscarPorNombreGlobal(nombreHija);

            if (globalHija != null)
            {
                return globalHija;
            }

            Categoria? padre = BuscarPorRuta(rutaPadre);

            if (padre == null)
            {
                padre = BuscarPorNombreGlobal(rutaPadre.Trim());
            }

            if (padre == null)
            {
                padre = BuscarOCrearPorRuta(rutaPadre);
            }

            Categoria? existente = padre.Hijos.BuscarPorNombre(nombreHija);

            if (existente != null)
            {
                return existente;
            }

            Categoria hija = new Categoria(nombreHija, padre);
            padre.Hijos.InsertarOrdenado(hija);
            totalCategorias++;
            return hija;
        }

        /// <summary>
        /// Busca una categoría por su ruta completa (ej: "Ciencia > Física > Mecánica").
        /// Si no existe, la crea iterativamente.
        /// Respeta unicidad global: antes de crear un intermedio verifica si el
        /// nombre ya existe en cualquier nivel y lo reutiliza como paso de la ruta.
        /// </summary>
        private Categoria BuscarOCrearPorRuta(string ruta)
        {
            string[] partes = ruta.Split('>');
            Categoria actual = raiz;

            for (int i = 0; i < partes.Length; i++)
            {
                string nombreLimpio = partes[i].Trim();

                if (string.IsNullOrEmpty(nombreLimpio))
                {
                    continue;
                }

                Categoria? encontrada = actual.Hijos.BuscarPorNombre(nombreLimpio);

                if (encontrada == null)
                {
                    // Reutilizar nodo global si ya existe (evita duplicados por ruta parcial)
                    encontrada = BuscarPorNombreGlobal(nombreLimpio);

                    if (encontrada == null)
                    {
                        encontrada = new Categoria(nombreLimpio, actual);
                        actual.Hijos.InsertarOrdenado(encontrada);
                        totalCategorias++;
                    }
                    else if (encontrada.Padre != actual)
                    {
                        // El nodo existe en otra rama: la ruta es inconsistente con la
                        // unicidad global. Se retorna el nodo global para no duplicar.
                        return encontrada;
                    }
                }

                actual = encontrada;
            }

            return actual;
        }

        /// <summary>
        /// Busca una categoría por ruta completa (ej: "Ciencia > Física").
        /// Retorna null si no existe.
        /// Si la ruta tiene un solo segmento, se busca globalmente por nombre
        /// (los nombres son únicos en todo el árbol).
        /// </summary>
        public Categoria? BuscarPorRuta(string ruta)
        {
            string[] partes = ruta.Split('>');
            Categoria actual = raiz;
            bool esPrimerSegmento = true;

            for (int i = 0; i < partes.Length; i++)
            {
                string nombreLimpio = partes[i].Trim();

                if (string.IsNullOrEmpty(nombreLimpio))
                {
                    continue;
                }

                // Ignorar prefijo de raíz virtual ("Biblioteca > ...")
                if (esPrimerSegmento && string.Equals(nombreLimpio, raiz.Nombre, StringComparison.OrdinalIgnoreCase))
                {
                    esPrimerSegmento = false;
                    continue;
                }

                esPrimerSegmento = false;

                Categoria? encontrada = actual.Hijos.BuscarPorNombre(nombreLimpio);

                if (encontrada == null)
                {
                    // Si es búsqueda de un solo nombre, intentar global
                    // (ej: libro XML referencia solo "Fantasía" aunque viva bajo "Ficción").
                    if (partes.Length == 1)
                    {
                        return BuscarPorNombreGlobal(nombreLimpio);
                    }

                    return null;
                }

                actual = encontrada;
            }

            // Si la ruta era solo la raíz virtual, retornar la raíz
            if (actual == raiz && esPrimerSegmento)
            {
                return raiz;
            }

            return actual;
        }

        /// <summary>
        /// Busca una categoría por nombre en todo el árbol (DFS, ignora mayúsculas).
        /// Los nombres son globalmente únicos según especificación.
        /// Retorna null si no existe.
        /// </summary>
        public Categoria? BuscarPorNombreGlobal(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return null;
            }

            string objetivo = nombre.Trim();
            return BuscarPorNombreGlobalRecursivo(raiz, objetivo);
        }

        private Categoria? BuscarPorNombreGlobalRecursivo(Categoria nodo, string objetivo)
        {
            if (nodo != raiz && string.Equals(nodo.Nombre, objetivo, StringComparison.OrdinalIgnoreCase))
            {
                return nodo;
            }

            Categoria[] hijos = nodo.Hijos.ObtenerTodas();
            for (int i = 0; i < hijos.Length; i++)
            {
                Categoria? hallada = BuscarPorNombreGlobalRecursivo(hijos[i], objetivo);
                if (hallada != null)
                {
                    return hallada;
                }
            }

            return null;
        }

        /// <summary>
        /// Reinicia el árbol a su estado inicial (solo raíz virtual).
        /// Se usa para la función de Inicialización de la interfaz.
        /// </summary>
        public void Reiniciar()
        {
            raiz = new Categoria("Biblioteca");
            totalCategorias = 1;
        }

        /// <summary>
        /// Elimina una categoría por ruta.
        /// No elimina si tiene libros o subcategorías.
        /// </summary>
        public bool EliminarCategoria(string ruta)
        {
            string[] partes = ruta.Split('>');
            Categoria actual = raiz;

            for (int i = 0; i < partes.Length - 1; i++)
            {
                string nombreLimpio = partes[i].Trim();
                Categoria? encontrada = actual.Hijos.BuscarPorNombre(nombreLimpio);

                if (encontrada == null)
                {
                    return false;
                }

                actual = encontrada;
            }

            string nombreEliminar = partes[partes.Length - 1].Trim();
            Categoria? aEliminar = actual.Hijos.BuscarPorNombre(nombreEliminar);

            if (aEliminar == null)
            {
                return false;
            }

            // No eliminar si tiene contenido
            if (!aEliminar.Hijos.EstaVacia() || aEliminar.Libros.ObtenerCantidad() > 0)
            {
                return false;
            }

            actual.Hijos.EliminarPorNombre(nombreEliminar);
            totalCategorias--;
            return true;
        }

        /// <summary>
        /// Retorna todas las categorías raíz (directamente bajo la raíz).
        /// </summary>
        public Categoria[] ObtenerCategoriasRaiz()
        {
            return raiz.Hijos.ObtenerTodas();
        }

        /// <summary>
        /// Retorna todas las categorías del árbol en orden preorden (profundidad primero).
        /// Útil para mostrar la estructura completa.
        /// </summary>
        public Categoria[] ObtenerTodasLasCategorias()
        {
            ListaSimpleCategoria lista = new ListaSimpleCategoria();
            RecorrerPreorden(raiz, lista);
            return lista.ObtenerArray();
        }

        private void RecorrerPreorden(Categoria nodo, ListaSimpleCategoria lista)
        {
            // No incluir la raíz virtual en la lista
            if (nodo != raiz)
            {
                lista.Agregar(nodo);
            }

            Categoria[] hijos = nodo.Hijos.ObtenerTodas();
            for (int i = 0; i < hijos.Length; i++)
            {
                RecorrerPreorden(hijos[i], lista);
            }
        }

        /// <summary>
        /// Genera una representación en texto de toda la estructura de categorías.
        /// Muestra la jerarquía con indentación.
        /// </summary>
        public string GenerarEstructuraTexto()
        {
            return GenerarTextoNodo(raiz, "", true, true);
        }

        /// <summary>
        /// Genera la estructura en texto a partir de una categoría específica.
        /// </summary>
        public string GenerarEstructuraDesde(string ruta)
        {
            Categoria? categoria = BuscarPorRuta(ruta);

            if (categoria == null)
            {
                return "Categoría no encontrada: " + ruta;
            }

            return GenerarTextoNodo(categoria, "", true, false);
        }

        private string GenerarTextoNodo(Categoria nodo, string prefijo, bool esUltimo, bool esRaiz)
        {
            string resultado = "";

            if (!esRaiz)
            {
                resultado += prefijo + (esUltimo ? "└── " : "├── ")
                    + nodo.Nombre
                    + $" [{nodo.Libros.ObtenerCantidad()} libros]\n";
            }

            string nuevoPrefijo = esRaiz ? "" : prefijo + (esUltimo ? "    " : "│   ");

            Categoria[] hijos = nodo.Hijos.ObtenerTodas();

            for (int i = 0; i < hijos.Length; i++)
            {
                bool esUltimoHijo = (i == hijos.Length - 1);
                resultado += GenerarTextoNodo(hijos[i], nuevoPrefijo, esUltimoHijo, false);
            }

            return resultado;
        }

        public int ObtenerTotalCategorias()
        {
            return totalCategorias;
        }
    }

    /// <summary>
    /// Lista enlazada simple concreta para categorías (TDA propio, sin genéricos).
    /// Uso interno del árbol de categorías para recorridos preorden.
    /// </summary>
    public class ListaSimpleCategoria
    {
        private NodoSimpleCategoria? cabeza;
        private int contador;

        public ListaSimpleCategoria()
        {
            cabeza = null;
            contador = 0;
        }

        public void Agregar(Categoria elemento)
        {
            NodoSimpleCategoria nuevo = new NodoSimpleCategoria(elemento);

            if (cabeza == null)
            {
                cabeza = nuevo;
            }
            else
            {
                NodoSimpleCategoria actual = cabeza;
                while (actual.Siguiente != null)
                {
                    actual = actual.Siguiente;
                }
                actual.Siguiente = nuevo;
            }

            contador++;
        }

        public Categoria[] ObtenerArray()
        {
            Categoria[] array = new Categoria[contador];
            NodoSimpleCategoria? actual = cabeza;
            int i = 0;

            while (actual != null)
            {
                array[i] = actual.Dato;
                i++;
                actual = actual.Siguiente;
            }

            return array;
        }

        public int ObtenerCantidad()
        {
            return contador;
        }
    }

    /// <summary>
    /// Nodo concreto para lista enlazada simple de categorías (sin genéricos).
    /// </summary>
    public class NodoSimpleCategoria
    {
        public Categoria Dato { get; set; }
        public NodoSimpleCategoria? Siguiente { get; set; }

        public NodoSimpleCategoria(Categoria dato)
        {
            Dato = dato;
            Siguiente = null;
        }
    }
}
