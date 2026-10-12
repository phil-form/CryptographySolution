using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CryptographySolution.Models;

public record User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id { get; set; }
    [Required]
    public string username { get; set; }
    [Required]
    public string password { get; set; }
    [Required]
    public string secretMessage { get; set; }
    // Générées par le serveur : ne doivent pas être exigées dans le body des requêtes.
    [ValidateNever]
    public string privateKey { get; set; }
    [ValidateNever]
    public string publicKey { get; set; }
}