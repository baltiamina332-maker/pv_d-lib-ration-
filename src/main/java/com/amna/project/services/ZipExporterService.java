package com.amna.project.services;

import org.springframework.stereotype.Service;

import java.io.ByteArrayOutputStream;
import java.util.Map;
import java.util.zip.ZipEntry;
import java.util.zip.ZipOutputStream;

@Service
public class ZipExporterService {

    public byte[] createZipArchive(Map<String, byte[]> docxFilesMap) throws Exception {
        try (ByteArrayOutputStream baos = new ByteArrayOutputStream();
             ZipOutputStream zos = new ZipOutputStream(baos)) {

            for (Map.Entry<String, byte[]> entry : docxFilesMap.entrySet()) {
                String filename = entry.getKey();
                if (!filename.endsWith(".docx")) {
                    filename += ".docx";
                }
                
                ZipEntry zipEntry = new ZipEntry(filename);
                zos.putNextEntry(zipEntry);
                zos.write(entry.getValue());
                zos.closeEntry();
            }

            zos.finish();
            return baos.toByteArray();
        }
    }
}
