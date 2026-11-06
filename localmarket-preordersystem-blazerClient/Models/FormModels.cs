using System.ComponentModel.DataAnnotations;

namespace localmarket_preordersystem.Models;

// Kliensoldali űrlap-validáció (azonnali visszajelzés). A hiteles ellenőrzést a szerver végzi, annak hibái is megjelennek.

public class LoginForm
{
    [Required(ErrorMessage = "Az e-mail cím megadása kötelező.")]
    [EmailAddress(ErrorMessage = "Érvénytelen e-mail cím formátum.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "A jelszó megadása kötelező.")]
    public string Password { get; set; } = "";
}

public class RegisterForm
{
    [Required(ErrorMessage = "A vezetéknév megadása kötelező.")]
    [StringLength(100, ErrorMessage = "Legfeljebb 100 karakter lehet.")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "A keresztnév megadása kötelező.")]
    [StringLength(100, ErrorMessage = "Legfeljebb 100 karakter lehet.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Az e-mail cím megadása kötelező.")]
    [EmailAddress(ErrorMessage = "Érvénytelen e-mail cím formátum.")]
    public string Email { get; set; } = "";

    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "A jelszó megadása kötelező.")]
    [MinLength(8, ErrorMessage = "A jelszónak legalább 8 karakter hosszúnak kell lennie.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Told meg a jelszót még egyszer.")]
    [Compare(nameof(Password), ErrorMessage = "A két jelszó nem egyezik.")]
    public string PasswordConfirm { get; set; } = "";

    // csak árusnál
    public string ProducerName { get; set; } = "";
    public int? MarketId { get; set; }
    public int StallNumber { get; set; }
}
