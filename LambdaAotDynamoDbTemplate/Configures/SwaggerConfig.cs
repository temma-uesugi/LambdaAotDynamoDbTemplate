using Microsoft.OpenApi;

namespace Configures;

/// <summary>
/// Swagger関係
/// </summary>
public static class SwaggerConfig
{
    /// <summary>
    /// 定義
    /// </summary>
    public static void ConfigureSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            // Note: Cookie認証を使う場合、このBearerトークンUIはどのエンドポイントからも参照されない。
            // 有効にするとSwagger UI上の全エンドポイントに鍵アイコンが付くため、使わない構成ではコメントアウトしておく。
            // Bearerヘッダ方式（Services/JwtTokenService.csのTryGetUserIdAsync(string?)）を使う案件では、
            // コメントを解除するだけで復活できる。
            // c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
            // {
            //     Name = "Authorization",
            //     Type = SecuritySchemeType.Http,
            //     Scheme = "bearer",
            //     BearerFormat = "JWT",
            //     In = ParameterLocation.Header,
            //     Description = "トークンを 'Bearer {token}' 形式で入力してください。",
            // });
            //
            // // Microsoft.OpenApi 2.x では OpenApiSecuritySchemeReference の生成にOpenApiDocumentが必要なため、
            // // ドキュメント構築時に呼ばれるデリゲート形式で登録する。
            // c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            // {
            //     [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            // });
        });
    }
}
