namespace ChonkyWeb.Helpers
{
    using AutoMapper;
    using ChonkyWeb.Models;
    using ChonkyWeb.Models.V1ApiModels;
    using StockDataLibrary.Models;
    using System.Collections.Generic;
    using System.Linq;

    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Account, AccountApiResponse>();
            CreateMap<TdaStockQuote, Quote>();
            CreateMap<List<TdaStockQuote>, APIQuotes>()
                .ForMember(dest => dest.Info, opt => opt.MapFrom<QuoteInfoConverter>())
                .ForMember(dest => dest.Quotes, opt => opt.MapFrom<QuotesConverter>());
        }

        public class QuotesConverter : IValueResolver<List<TdaStockQuote>, APIQuotes, List<Quote>>
        {
            public List<Quote> Resolve(List<TdaStockQuote> source, APIQuotes destination, List<Quote> destMember, ResolutionContext context)
            {
                return source.Select(q => { return context.Mapper.Map<TdaStockQuote, Quote>(q); }).ToList();
            }
        }

        public class QuoteInfoConverter : IValueResolver<List<TdaStockQuote>, APIQuotes, QuoteInfo>
        {
            public QuoteInfo Resolve(List<TdaStockQuote> source, APIQuotes destination, QuoteInfo destMember, ResolutionContext context)
            {
                if (source.Count == 0)
                    return new QuoteInfo();
                return new QuoteInfo { Symbol = source[0].Symbol };
            }
        }
    }
}
