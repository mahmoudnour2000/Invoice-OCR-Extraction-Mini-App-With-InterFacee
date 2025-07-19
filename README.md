# Invoice OCR Extraction Mini App

A .NET 9 web application that extracts data from invoice images using OCR (Optical Character Recognition) technology.

## Features

- Upload invoice images (PDF, JPG, PNG)
- Extract invoice data using Tesseract OCR
- Parse invoice details including:
  - Invoice number and date
  - Vendor information
  - Line items with quantities and prices
  - Subtotal, VAT, and total amounts
- Store extracted data in SQLite database
- RESTful API endpoints

## Technologies Used

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- SQLite Database
- Tesseract OCR
- iText7 (PDF processing)
- SixLabors.ImageSharp (Image processing)

## Project Structure

- `InvoiceOcrApp` - Main web application
- `InvoiceOcr.Models` - Data models
- `InvoiceOcr.Data` - Database context and configurations
- `InvoiceOcr.Services` - Business logic and OCR services
- `InvoiceOcr.Repositories` - Data access layer
- `InvoiceOcr.DTOs` - Data transfer objects

## Prerequisites

- .NET 9 SDK
- Tesseract OCR language data files (tessdata)

## Setup

1. Clone the repository
2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
3. Ensure tessdata folder exists with English language files
4. Run the application:
   ```bash
   dotnet run --project InvoiceOcrApp
   ```
## SQL generate 
  ```bash
  Add-Migration "Name Of Migration"
  ```
  ```bash
  Update-Database
  ```
  write this command In Package Manager Console In Visual Studio 
  
## API Endpoints

### Upload Controller
- `POST /api/Upload` - Upload and process invoice image/PDF file

### Invoice Controller
- `POST /api/Invoice` - Create a new invoice manually
- `GET /api/Invoice/{id}` - Get specific invoice by ID
- `GET /api/Invoice/customer/{customerName}` - Get invoices by customer name
- `GET /api/Invoice/details/{invoiceId}` - Get invoice details by invoice ID
- `PUT /api/Invoice` - Update existing invoice

## Configuration

The application uses SQLite database by default. Connection string can be configured in `appsettings.json`.

## License

This project is for educational/demonstration purposes.
