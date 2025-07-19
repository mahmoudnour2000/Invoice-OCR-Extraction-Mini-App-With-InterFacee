using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvoiceOcr.Model;
using InvoiceOcr.Model;

namespace InvoiceOcr.DTOs.Mappers
{
    public static class MappingExtensions
    {
        #region Invoice Mapping
        public static Invoice ToEntity(this InvoiceDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new Invoice
            {
                Id = dto.Id,
                InvoiceNumber = dto.InvoiceNumber ?? string.Empty,
                InvoiceDate = dto.InvoiceDate,
                CustomerName = dto.CustomerName ?? string.Empty,
                TotalAmount = dto.TotalAmount,
                Vat = dto.Vat,
                Details = dto.Details?.Select(d => d.ToEntity()).ToList() ?? new List<InvoiceDetail>()
            };
        }

        public static InvoiceDto ToDto(this Invoice entity)
        {
            if (entity == null) return null;
            return new InvoiceDto
            {
                Id = entity.Id,
                InvoiceNumber = entity.InvoiceNumber,
                InvoiceDate = entity.InvoiceDate,
                CustomerName = entity.CustomerName,
                TotalAmount = entity.TotalAmount,
                Vat = entity.Vat,
                Details = entity.Details?.Select(d => d.ToDto()).ToList() ?? new List<InvoiceDetailDto>()
            };
        }
        #endregion

        #region Invoice Detail Mapping
        public static InvoiceDetail ToEntity(this InvoiceDetailDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            return new InvoiceDetail
            {
                Id = dto.Id,
                InvoiceId = dto.InvoiceId,
                Description = dto.Description ?? string.Empty,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                LineTotal = dto.LineTotal
            };
        }

        public static InvoiceDetailDto ToDto(this InvoiceDetail entity)
        {
            if (entity == null) return null;
            return new InvoiceDetailDto
            {
                Id = entity.Id,
                InvoiceId = entity.InvoiceId,
                Description = entity.Description,
                Quantity = entity.Quantity,
                UnitPrice = entity.UnitPrice,
                LineTotal = entity.LineTotal
            };
        }
        #endregion

        #region Collection Mapping
        public static List<InvoiceDto> ToDtoList(this IEnumerable<Invoice> entities)
        {
            return entities?.Select(e => e.ToDto()).Where(d => d != null).ToList() ?? new List<InvoiceDto>();
        }

        public static List<InvoiceDetailDto> ToDtoList(this IEnumerable<InvoiceDetail> entities)
        {
            return entities?.Select(e => e.ToDto()).Where(d => d != null).ToList() ?? new List<InvoiceDetailDto>();
        }
        #endregion
    }
}
