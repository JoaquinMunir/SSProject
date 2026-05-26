using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TIProject.Data
{
    public class Documento
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Category { get; set; } = "General";
        public string Type { get; set; } = "PDF";
        public string Size { get; set; } = "0 KB";
        public DateTime UploadDate { get; set; }
    }

    /// <summary>
    /// Resultado del análisis previo de seguridad (client-side, sin leer el stream).
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; init; }
        public string? Error { get; init; }
        public List<string> Warnings { get; init; } = new();

        /// <summary>safe | caution | danger</summary>
        public string SecurityLevel { get; init; } = "unknown";
    }

    public class Documents
    {
        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();

        private readonly IWebHostEnvironment _env;
        private List<Documento> _documentos = new();
        private readonly string _dbPath;
        private int _nextId = 1;

        // ─── Listas de control ───────────────────────────────────────────────

        /// <summary>Whitelist de extensiones permitidas.</summary>
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt", ".csv"
        };

        /// <summary>
        /// Extensiones de ejecutables/scripts. Usadas para detectar dobles extensiones
        /// ocultas (ej. "malware.exe.pdf").
        /// </summary>
        private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "exe", "bat", "cmd", "sh", "ps1", "vbs", "js", "jar",
            "com", "scr", "pif", "msi", "dll", "cpl", "hta", "wsf", "vbe"
        };

        /// <summary>
        /// Formatos Office heredados que admiten macros VBA.
        /// Se permite su carga pero se muestra advertencia.
        /// </summary>
        private static readonly HashSet<string> MacroCapableExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".doc", ".xls", ".ppt" };

        // ─── Firmas mágicas (magic bytes) ───────────────────────────────────

        private static readonly Dictionary<string, byte[]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf",  new byte[] { 0x25, 0x50, 0x44, 0x46 } },        // %PDF
            { ".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47 } },        // PNG
            { ".jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },               // JPEG
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },               // JPEG
            { ".gif",  new byte[] { 0x47, 0x49, 0x46 } },               // GIF87a / GIF89a
            { ".bmp",  new byte[] { 0x42, 0x4D } },                     // BM
            { ".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },        // Office Open XML (ZIP)
            { ".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            { ".pptx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            { ".doc",  new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },        // OLE2 (Office 97-2003)
            { ".xls",  new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },
            { ".ppt",  new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },
        };

        /// <summary>Firma PE (Windows Portable Executable). Detecta ejecutables camuflados.</summary>
        private static readonly byte[] PeHeader = { 0x4D, 0x5A }; // MZ

        // ─── Constructor ─────────────────────────────────────────────────────

        public Documents(IWebHostEnvironment env)
        {
            _env = env;
            _dbPath = Path.Combine(_env.ContentRootPath, "Data", "documentos_db.json");
            CargarDatos();
            if (_documentos.Any())
                _nextId = _documentos.Max(d => d.Id) + 1;
        }

        // ─── Validación previa (sin leer el stream) ──────────────────────────

        /// <summary>
        /// Análisis previo de seguridad ejecutado antes de leer el contenido del archivo.
        /// Verifica: nombre, doble extensión oculta, caracteres peligrosos, tamaño y
        /// extensión contra la whitelist.
        /// </summary>
        public static FileValidationResult PreValidate(string fileName, long fileSize)
        {
            // 1. Caracteres de control / null bytes en el nombre
            if (fileName.Any(c => c < 0x20))
                return Danger("El nombre del archivo contiene caracteres de control no permitidos.");

            // 2. Path traversal y caracteres inválidos en el nombre
            if (fileName.Contains("..") || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return Danger("El nombre del archivo contiene caracteres no permitidos ('..', '/', '\\', etc.).");

            // 3. Detección de doble extensión oculta (ej. "virus.exe.pdf")
            //    Se analiza el nombre sin la extensión final y se busca alguna parte
            //    que coincida con extensiones de ejecutables/scripts.
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var nameParts = nameWithoutExt.Split('.');
            foreach (var part in nameParts)
            {
                if (DangerousExtensions.Contains(part))
                    return Danger(
                        $"Nombre sospechoso: se detectó la extensión peligrosa oculta '.{part}' " +
                        $"en '{fileName}'. El archivo fue rechazado.");
            }

            // 4. Límite de tamaño (20 MB)
            const long maxBytes = 20L * 1024 * 1024;
            if (fileSize > maxBytes)
                return Danger(
                    $"El archivo supera el límite permitido de 20 MB " +
                    $"(tamaño detectado: {fileSize / 1024.0 / 1024.0:F1} MB).");

            // 5. Extensión contra la whitelist
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return Danger(
                    $"Tipo de archivo no permitido: '{ext}'. " +
                    "Tipos aceptados: PDF, Word, Excel, PowerPoint, imágenes (PNG/JPG/GIF/BMP), TXT, CSV.");

            // 6. Advertencia para formatos con soporte de macros VBA
            var warnings = new List<string>();
            if (MacroCapableExtensions.Contains(ext))
                warnings.Add(
                    $"El formato '{ext}' puede contener macros VBA. " +
                    "Si el documento proviene de una fuente desconocida, verifica su origen antes de abrirlo. " +
                    "Prefiere el formato moderno (.docx / .xlsx / .pptx).");

            var level = warnings.Count > 0 ? "caution" : "safe";
            return new FileValidationResult { IsValid = true, Warnings = warnings, SecurityLevel = level };
        }

        // ─── Carga de archivo (validación completa de contenido) ─────────────

        /// <summary>
        /// Sube un archivo al servidor aplicando validación completa:
        /// extensión, tamaño, doble extensión, firma PE (ejecutables camuflados)
        /// y magic bytes (contenido real vs. extensión declarada).
        /// </summary>
        /// <summary>
        /// Valida y guarda un archivo recibido como arreglo de bytes.
        /// Los bytes deben haberse leído del IBrowserFile durante el mismo evento
        /// de selección (OnChange), no después.
        /// </summary>
        public async Task UploadFile(byte[] fileData, string fileName, string title, string category)
        {
            // --- Re-validación server-side ---
            var pre = PreValidate(fileName, fileData.Length);
            if (!pre.IsValid)
                throw new InvalidOperationException(pre.Error);

            if (fileData.Length == 0)
                throw new InvalidOperationException("El archivo está vacío.");

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            // --- Detección de ejecutable camuflado (firma PE: MZ = 0x4D 0x5A) ---
            if (fileData.Length >= 2)
            {
                var peSlice = new byte[] { fileData[0], fileData[1] };
                if (peSlice.SequenceEqual(PeHeader))
                    throw new InvalidOperationException(
                        "El archivo fue identificado como un ejecutable de Windows (firma MZ). " +
                        "La carga fue rechazada por motivos de seguridad.");
            }

            // --- Validación de magic bytes (contenido real vs. extensión declarada) ---
            if (MagicBytes.TryGetValue(extension, out var magic))
            {
                if (fileData.Length < magic.Length)
                    throw new InvalidOperationException("El archivo está vacío o corrupto.");

                var header = fileData.Take(magic.Length).ToArray();
                if (!header.SequenceEqual(magic))
                    throw new InvalidOperationException(
                        $"El contenido del archivo no corresponde a un '{extension}' legítimo. " +
                        "Posible archivo renombrado para ocultar su tipo real.");
            }

            // --- Guardar en disco ---
            string uploadsFolder = ResolveUploadsFolder();

            var physicalName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, physicalName);

            await File.WriteAllBytesAsync(filePath, fileData);

            // --- Registrar y persistir ---
            _documentos.Add(new Documento
            {
                Id = _nextId++,
                Title = string.IsNullOrWhiteSpace(title) ? fileName : title,
                FileName = physicalName,
                Category = category,
                Type = extension.TrimStart('.').ToUpper(),
                Size = FormatSize(fileData.Length),
                UploadDate = DateTime.Now
            });

            GuardarDatos();
            NotifyStateChanged();
        }

        /// <summary>
        /// Resuelve la carpeta de uploads de forma resiliente.
        /// Intenta: WebRootPath → ContentRootPath/wwwroot → directorio actual/wwwroot.
        /// </summary>
        private string ResolveUploadsFolder()
        {
            var candidates = new[]
            {
                _env.WebRootPath,
                Path.Combine(_env.ContentRootPath, "wwwroot"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            };

            foreach (var root in candidates)
            {
                if (string.IsNullOrWhiteSpace(root)) continue;
                try
                {
                    var uploads = Path.Combine(root, "uploads");
                    Directory.CreateDirectory(uploads);
                    // Probar escritura real
                    var probe = Path.Combine(uploads, $".probe_{Guid.NewGuid()}");
                    File.WriteAllText(probe, "");
                    File.Delete(probe);
                    return uploads;
                }
                catch { /* probar siguiente candidato */ }
            }

            // Último recurso: carpeta temporal del sistema
            var tmp = Path.Combine(Path.GetTempPath(), "TIProject_uploads");
            Directory.CreateDirectory(tmp);
            return tmp;
        }

        // ─── CRUD ─────────────────────────────────────────────────────────────

        public List<Documento> GetAll() =>
            _documentos.OrderByDescending(d => d.UploadDate).ToList();

        public void Delete(Documento doc)
        {
            _documentos.Remove(doc);
            GuardarDatos();
            NotifyStateChanged();

            try
            {
                string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var path = Path.Combine(rootPath, "uploads", doc.FileName);
                if (File.Exists(path)) File.Delete(path);
            }
            catch { /* El archivo físico no es crítico si ya se eliminó del registro */ }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static FileValidationResult Danger(string error) =>
            new() { IsValid = false, Error = error, SecurityLevel = "danger" };

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / 1024.0 / 1024.0:F1} MB"
        };

        private void GuardarDatos()
        {
            try
            {
                var dir = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(_dbPath, JsonSerializer.Serialize(_documentos));
            }
            catch { }
        }

        private void CargarDatos()
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    var json = File.ReadAllText(_dbPath);
                    _documentos = JsonSerializer.Deserialize<List<Documento>>(json) ?? new();
                }
            }
            catch { _documentos = new(); }
        }
    }
}
