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

    public class Documents
    {
        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();

        private readonly IWebHostEnvironment _env;
        private List<Documento> _documentos = new();
        private string _dbPath;
        private int _nextId = 1;

        // Extensiones permitidas (whitelist)
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt", ".csv"
        };

        // Firmas mágicas (magic bytes) para validar el contenido real del archivo
        private static readonly Dictionary<string, byte[]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf",  new byte[] { 0x25, 0x50, 0x44, 0x46 } },       // %PDF
            { ".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47 } },       // PNG
            { ".jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },              // JPEG
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },              // JPEG
            { ".gif",  new byte[] { 0x47, 0x49, 0x46 } },              // GIF
            { ".bmp",  new byte[] { 0x42, 0x4D } },                    // BM
            // Office Open XML (docx, xlsx, pptx) son archivos ZIP
            { ".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },       // PK (ZIP)
            { ".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            { ".pptx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            // Office 97-2003 son archivos OLE2
            { ".doc",  new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },
            { ".xls",  new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },
            { ".ppt",  new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } },
        };

        public Documents(IWebHostEnvironment env)
        {
            _env = env;
            _dbPath = Path.Combine(_env.ContentRootPath, "Data", "documentos_db.json");
            CargarDatos();
            if (_documentos.Any())
                _nextId = _documentos.Max(d => d.Id) + 1;
        }

        public async Task UploadFile(IBrowserFile file, string title, string category)
        {
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();

            // 1. Validar extensión contra whitelist
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    $"Tipo de archivo no permitido: '{extension}'. " +
                    $"Tipos válidos: PDF, Word, Excel, PowerPoint, imágenes.");

            // 2. Leer el archivo en memoria para validar magic bytes y luego guardarlo
            using var ms = new MemoryStream();
            await using (var readStream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024))
            {
                await readStream.CopyToAsync(ms);
            }
            ms.Position = 0;

            // 3. Validar magic bytes (firma del contenido real)
            if (MagicBytes.TryGetValue(extension, out var magic))
            {
                if (ms.Length < magic.Length)
                    throw new InvalidOperationException("El archivo está vacío o corrupto.");

                var header = new byte[magic.Length];
                await ms.ReadAsync(header, 0, header.Length);
                ms.Position = 0;

                if (!header.SequenceEqual(magic))
                    throw new InvalidOperationException(
                        "El contenido del archivo no coincide con su extensión. " +
                        "Posible archivo malicioso o renombrado.");
            }

            // 4. Guardar en disco con nombre único (GUID) para evitar path traversal
            string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(rootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileNameFisico = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileNameFisico);

            await using var fs = new FileStream(filePath, FileMode.Create);
            await ms.CopyToAsync(fs);

            // 5. Registrar en memoria y persistir
            _documentos.Add(new Documento
            {
                Id = _nextId++,
                Title = string.IsNullOrWhiteSpace(title) ? file.Name : title,
                FileName = fileNameFisico,
                Category = category,
                Type = extension.Replace(".", "").ToUpper(),
                Size = $"{file.Size / 1024} KB",
                UploadDate = DateTime.Now
            });

            GuardarDatos();
            NotifyStateChanged();
        }

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
            catch { }
        }

        private void GuardarDatos()
        {
            try
            {
                var dir = Path.GetDirectoryName(_dbPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
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
