package com.amna.project.dto;

import lombok.Data;

@Data
public class SignupRequestDTO {
    private String username;
    private String password;
    private String email;
}
