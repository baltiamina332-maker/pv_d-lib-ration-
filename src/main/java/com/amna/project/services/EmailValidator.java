package com.amna.project.services;

import java.util.regex.Pattern;

/**
 * Validation simple du format d'une adresse e-mail.
 */
public final class EmailValidator {

    private static final Pattern EMAIL = Pattern.compile("^[\\w.+'-]+@[\\w-]+(\\.[\\w-]+)*\\.[A-Za-z]{2,}$");

    private EmailValidator() {
    }

    public static boolean isValid(String email) {
        return email != null && EMAIL.matcher(email.trim()).matches();
    }
}
