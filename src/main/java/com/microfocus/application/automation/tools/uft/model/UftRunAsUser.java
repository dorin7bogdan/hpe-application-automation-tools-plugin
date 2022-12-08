package com.microfocus.application.automation.tools.uft.model;

import com.microfocus.application.automation.tools.EncryptionUtils;
import hudson.model.Node;
import hudson.util.Secret;
import org.apache.commons.lang.StringUtils;

import static com.microfocus.application.automation.tools.uft.utils.Constants.*;

public class UftRunAsUser {
    private String username;
    private String domain;
    private Secret password;

    private String encPassword;

    private final String ARG_IS_REQUIRED = "%s is required";
    public UftRunAsUser(String username, String domain, Secret password, Node node) throws EncryptionUtils.EncryptionException {
        if (StringUtils.isBlank(username) ) {
            throw new IllegalArgumentException(String.format(ARG_IS_REQUIRED, UFT_RUN_AS_USER));
        } else if (StringUtils.isBlank(domain)) {
            throw new IllegalArgumentException(String.format(ARG_IS_REQUIRED, UFT_RUN_AS_DOMAIN));
        } else if (password == null || StringUtils.isBlank(password.getPlainText()) ) {
            throw new IllegalArgumentException(String.format(ARG_IS_REQUIRED, UFT_RUN_AS_PWD));
        }
        this.username = username;
        this.domain = domain;
        this.password = password;
        this.encPassword = EncryptionUtils.encrypt(password.getPlainText(), node);
    }

    public String getUsername() {
        return username;
    }

    public String getDomain() {
        return domain;
    }

    public Secret getPassword() {
        return password;
    }

    public String getEncryptedPassword() { return encPassword; }
}
